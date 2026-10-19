using System.Text;
using System.Threading.RateLimiting;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Api;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Contracts.Routes;
using Vitarish.Requests.Application.Abstractions;
using BaseModule = Vitarish.Modules.Base.Core.Module;

namespace Parties.Tests.Api;

public sealed class CreateOwnTenantEndpointTests
{
    private const string DefaultBody = "{\"name\":\"Clinic\",\"subdomain\":\"rezaei\",\"description\":null}";
    private const string ValidIdempotencyKey = "idem-key-1234";

    [Fact]
    public void Requires_an_authenticated_user_without_tenant_access_and_declares_rate_limiting()
    {
        using WebApplication app = CreateMappedApp(
            onboardingEnabled: true,
            baseDomain: "clinic.example.test",
            registerProfile: true,
            userId: "user-1",
            Mock.Of<ISender>());
        RouteEndpoint endpoint = FindEndpoint(app);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer)
            .And.NotContain(metadata => metadata.Policy == IdentityDefinitions.TenantAccess);
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods
            .Should().ContainSingle().Which.Should().Be(HttpMethods.Post);
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route
            .Should().Contain($"{PartiesApiRoutes.Base}{PartiesApiRoutes.Onboarding}{PartiesApiRoutes.OnboardingTenants}");
        endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be(PartiesRateLimitPolicies.SelfServiceTenantCreation);
    }

    [Theory]
    [InlineData(false, "clinic.example.test", true)]
    [InlineData(true, null, true)]
    [InlineData(true, " ", true)]
    [InlineData(true, "clinic.example.test", false)]
    public async Task Answers_not_found_when_onboarding_is_off_or_missing_base_domain_or_profile(
        bool onboardingEnabled,
        string? baseDomain,
        bool registerProfile)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(onboardingEnabled, baseDomain, registerProfile, "user-1", sender.Object);

        HttpContext context = await InvokeAsync(app, DefaultBody, ValidIdempotencyKey);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("bad key!!")]
    public async Task Rejects_a_missing_or_malformed_idempotency_key_at_transport(string? idempotencyKey)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(
            onboardingEnabled: true,
            baseDomain: "clinic.example.test",
            registerProfile: true,
            userId: "user-1",
            sender.Object);

        HttpContext context = await InvokeAsync(app, DefaultBody, idempotencyKey);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Answers_unauthorized_when_the_request_context_has_no_user_id()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(
            onboardingEnabled: true,
            baseDomain: "clinic.example.test",
            registerProfile: true,
            userId: null,
            sender.Object);

        HttpContext context = await InvokeAsync(app, DefaultBody, ValidIdempotencyKey);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Returns_created_with_tenant_id_and_hostname_and_uses_the_session_user_id()
    {
        Guid tenantId = Guid.NewGuid();
        CreateOwnTenantCommand? captured = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<CreateOwnTenantCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CreateOwnTenantResult>, CancellationToken>((command, _) => captured = (CreateOwnTenantCommand)command)
            .ReturnsAsync(new CreateOwnTenantResult(CreateOwnTenantStatus.Created, tenantId, "rezaei.clinic.example.test"));

        using WebApplication app = CreateMappedApp(
            onboardingEnabled: true,
            baseDomain: "clinic.example.test",
            registerProfile: true,
            userId: "user-1",
            sender.Object);

        HttpContext context = await InvokeAsync(
            app,
            "{\"name\":\"Clinic\",\"subdomain\":\"rezaei\",\"description\":null,\"creatorUserId\":\"attacker\"}",
            ValidIdempotencyKey);

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        captured.Should().NotBeNull();
        captured!.CreatorUserId.Should().Be("user-1");
        captured.Name.Should().Be("Clinic");
        captured.Subdomain.Should().Be("rezaei");
        captured.IdempotencyKey.Should().Be(ValidIdempotencyKey);

        context.Response.Body.Position = 0;
        string body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain(tenantId.ToString()).And.Contain("rezaei.clinic.example.test");
    }

    [Theory]
    [InlineData(CreateOwnTenantStatus.Replayed, StatusCodes.Status200OK)]
    [InlineData(CreateOwnTenantStatus.AddressTaken, StatusCodes.Status409Conflict)]
    [InlineData(CreateOwnTenantStatus.PhoneNotVerified, StatusCodes.Status403Forbidden)]
    [InlineData(CreateOwnTenantStatus.LimitReached, StatusCodes.Status429TooManyRequests)]
    [InlineData(CreateOwnTenantStatus.AddressReserved, StatusCodes.Status400BadRequest)]
    public async Task Maps_application_statuses_to_http_status_codes(CreateOwnTenantStatus status, int expectedStatusCode)
    {
        Guid tenantId = Guid.NewGuid();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(
                new CreateOwnTenantCommand("user-1", "Clinic", "rezaei", null, ValidIdempotencyKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOwnTenantResult(status, tenantId, "rezaei.clinic.example.test"));

        using WebApplication app = CreateMappedApp(
            onboardingEnabled: true,
            baseDomain: "clinic.example.test",
            registerProfile: true,
            userId: "user-1",
            sender.Object);

        HttpContext context = await InvokeAsync(app, DefaultBody, ValidIdempotencyKey);

        context.Response.StatusCode.Should().Be(expectedStatusCode);
    }

    private static WebApplication CreateMappedApp(
        bool onboardingEnabled,
        string? baseDomain,
        bool registerProfile,
        string? userId,
        ISender sender)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [BaseModule.TenantOnboardingFeatureKey] = onboardingEnabled.ToString(),
        });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(options =>
            options.AddPolicy(PartiesRateLimitPolicies.SelfServiceTenantCreation, _ => RateLimitPartition.GetNoLimiter("x")));
        builder.Services.AddSingleton(Mock.Of<IPartiesService>());
        builder.Services.AddSingleton(Mock.Of<ISecurityAuditWriter>());
        builder.Services.AddSingleton(sender);
        builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        builder.Services.AddOptions<TenantAddressOptions>().Configure(options => options.TenantBaseDomain = baseDomain);

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns(userId);
        builder.Services.AddSingleton(requestContext.Object);

        if (registerProfile)
            builder.Services.AddSingleton(new SelfServiceTenantProfile("clinic-app", ["Clinic"]));

        WebApplication app = builder.Build();
        _ = app.MapPartiesApi();
        return app;
    }

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string jsonBody, string? idempotencyKey)
    {
        RouteEndpoint endpoint = FindEndpoint(app);
        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        byte[] payload = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(payload);
        context.Request.ContentLength = payload.Length;
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature());
        if (idempotencyKey is not null)
            context.Request.Headers[PartiesApiRoutes.IdempotencyKeyHeader] = idempotencyKey;
        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        return context;
    }

    private static RouteEndpoint FindEndpoint(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == PartiesApiOperations.CreateOwnTenant);

    private sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
