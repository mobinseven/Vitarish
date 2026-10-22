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
using Vitarish.Parties.Api;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Contracts.Routes;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using BaseModule = Vitarish.Modules.Base.Core.Module;

namespace Parties.Tests.Api;

public sealed class TenantBusinessProfileEndpointTests
{
    private const string ValidDetails =
        "{\"address\":{\"province\":\"Tehran\",\"latitude\":35.7,\"longitude\":51.4},\"phones\":[\"021 1234 5678\"],"
        + "\"workingHours\":[{\"day\":\"Saturday\",\"intervals\":[{\"opens\":\"09:00\",\"closes\":\"13:00\"}]}]}";

    [Theory]
    [InlineData(PartiesApiOperations.GetTenantProfile, "GET")]
    [InlineData(PartiesApiOperations.UpdateTenantProfile, "PUT")]
    [InlineData(PartiesApiOperations.SetTenantLogo, "PUT")]
    [InlineData(PartiesApiOperations.DismissTenantLogoPrompt, "PUT")]
    public void Owner_endpoints_require_the_tenant_owner_policy_and_tenant_access(string operationId, string method)
    {
        using WebApplication app = CreateApp(Mock.Of<ISender>());
        RouteEndpoint endpoint = FindEndpoint(app, operationId);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.Policy == IdentityDefinitions.TenantOwner)
            .And.Contain(metadata => metadata.Policy == IdentityDefinitions.TenantAccess);
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().Equal(method);
    }

    [Fact]
    public void Public_logo_is_anonymous_rate_limited_and_justified()
    {
        using WebApplication app = CreateApp(Mock.Of<ISender>());
        RouteEndpoint endpoint = FindEndpoint(app, PartiesApiOperations.GetPublicTenantLogo);

        endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().BeEmpty();
        endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().BeNull();
        endpoint.Metadata.GetMetadata<AnonymousModuleEndpointMetadata>()!.Justification.Should().NotBeNullOrWhiteSpace();
        endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName.Should().Be(PartiesRateLimitPolicies.PublicLogo);
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().Equal(HttpMethods.Get);
        endpoint.RoutePattern.RawText.Should().Be(PartiesApiRoutes.PublicLogoPath);
    }

    [Fact]
    public async Task Serves_the_logo_as_its_image_type_and_answers_not_found_without_one()
    {
        Mock<ISender> withLogo = new();
        withLogo.Setup(item => item.Send(It.IsAny<GetPublicTenantLogoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantImageContent(new MemoryStream([1, 2, 3]), "image/png", 3));
        using WebApplication app = CreateApp(withLogo.Object);
        using WebApplication none = CreateApp(Mock.Of<ISender>());

        HttpContext served = await InvokeAsync(app, PartiesApiOperations.GetPublicTenantLogo, HttpMethods.Get);
        HttpContext missing = await InvokeAsync(none, PartiesApiOperations.GetPublicTenantLogo, HttpMethods.Get);

        served.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        served.Response.ContentType.Should().Be("image/png");
        ((MemoryStream)served.Response.Body).ToArray().Should().Equal(1, 2, 3);
        missing.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Lists_sign_in_clinics_with_the_public_logo_address_only_for_one_with_a_logo()
    {
        Guid withLogo = Guid.NewGuid();
        Guid withoutLogo = Guid.NewGuid();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<IsSignInHostQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sender.Setup(item => item.Send(It.IsAny<ListSignInClinicsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SignInClinic(withLogo, "A", "a.clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: true),
                new SignInClinic(withoutLogo, "B", "b.clinic.example.test", IdentityDefinitions.TenantUser, HasLogo: false)]);
        using WebApplication app = CreateApp(sender.Object);

        HttpContext context = await InvokeAsync(app, PartiesApiOperations.GetSignInClinics, HttpMethods.Get);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        (await ReadBodyAsync(context)).Should().Be(
            $"{{\"clinics\":[{{\"tenantId\":\"{withLogo}\",\"name\":\"A\",\"hostname\":\"a.clinic.example.test\",\"role\":\"{IdentityDefinitions.TenantOwner}\",\"logoPath\":\"{PartiesApiRoutes.PublicLogoPath}\"}},"
            + $"{{\"tenantId\":\"{withoutLogo}\",\"name\":\"B\",\"hostname\":\"b.clinic.example.test\",\"role\":\"{IdentityDefinitions.TenantUser}\",\"logoPath\":null}}]}}");
    }

    [Fact]
    public async Task Lists_the_apps_categories_only_while_onboarding_is_available()
    {
        using WebApplication on = CreateApp(Mock.Of<ISender>());
        using WebApplication off = CreateApp(Mock.Of<ISender>(), onboardingEnabled: false);
        using WebApplication noProfile = CreateApp(Mock.Of<ISender>(), registerProfile: false);

        HttpContext listed = await InvokeAsync(on, PartiesApiOperations.GetOnboardingCategories, HttpMethods.Get);
        HttpContext hidden = await InvokeAsync(off, PartiesApiOperations.GetOnboardingCategories, HttpMethods.Get);
        HttpContext unregistered = await InvokeAsync(noProfile, PartiesApiOperations.GetOnboardingCategories, HttpMethods.Get);

        listed.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        (await ReadBodyAsync(listed)).Should().Be("{\"categories\":[\"dermatology\",\"other\"]}");
        hidden.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        unregistered.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData("{\"opens\":\"9am\",\"closes\":\"13:00\"}")]
    [InlineData("{\"opens\":\"13:00\",\"closes\":\"09:00\"}")]
    public async Task Rejects_invalid_opening_hours_at_the_boundary_without_sending_a_command(string interval)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateApp(sender.Object);
        string body = "{\"description\":null,\"category\":null,\"businessDetails\":{\"workingHours\":[{\"day\":\"Monday\",\"intervals\":[" + interval + "]}]}}";

        HttpContext context = await InvokeAsync(app, PartiesApiOperations.UpdateTenantProfile, HttpMethods.Put, body);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(TenantBusinessProfileUpdateStatus.Updated, StatusCodes.Status204NoContent)]
    [InlineData(TenantBusinessProfileUpdateStatus.Invalid, StatusCodes.Status400BadRequest)]
    [InlineData(TenantBusinessProfileUpdateStatus.NotFound, StatusCodes.Status404NotFound)]
    public async Task Maps_profile_update_outcomes(TenantBusinessProfileUpdateStatus status, int expectedStatusCode)
    {
        UpdateTenantBusinessProfileCommand? captured = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<UpdateTenantBusinessProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TenantBusinessProfileUpdateResult>, CancellationToken>((command, _) => captured = (UpdateTenantBusinessProfileCommand)command)
            .ReturnsAsync(new TenantBusinessProfileUpdateResult(status));
        using WebApplication app = CreateApp(sender.Object);

        HttpContext context = await InvokeAsync(
            app,
            PartiesApiOperations.UpdateTenantProfile,
            HttpMethods.Put,
            "{\"description\":\"Skin care\",\"category\":\"dermatology\",\"businessDetails\":" + ValidDetails + "}");

        context.Response.StatusCode.Should().Be(expectedStatusCode);
        captured!.Category.Should().Be("dermatology");
        captured.Details!.WorkingHours.Single().Intervals.Single().Should().Be(new TenantOpeningInterval(new TimeOnly(9, 0), new TimeOnly(13, 0)));
    }

    [Fact]
    public async Task Returns_the_profile_with_times_in_hours_and_minutes()
    {
        TenantBusinessDetails details = TenantBusinessDetails.TryCreate(
            null, null, null, null,
            [new TenantWorkingDay(DayOfWeek.Saturday, [new TenantOpeningInterval(new TimeOnly(9, 0), new TimeOnly(13, 30))])],
            null, out _)!;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<GetTenantBusinessProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantBusinessProfileView("Rezaei Clinic", null, null, "dermatology", details, true));
        using WebApplication app = CreateApp(sender.Object);

        HttpContext context = await InvokeAsync(app, PartiesApiOperations.GetTenantProfile, HttpMethods.Get);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        (await ReadBodyAsync(context)).Should().Contain("{\"day\":\"Saturday\",\"intervals\":[{\"opens\":\"09:00\",\"closes\":\"13:30\"}]}");
    }

    [Fact]
    public async Task Create_passes_category_and_details_and_rejects_invalid_details_before_sending()
    {
        CreateOwnTenantCommand? captured = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<CreateOwnTenantCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CreateOwnTenantResult>, CancellationToken>((command, _) => captured = (CreateOwnTenantCommand)command)
            .ReturnsAsync(new CreateOwnTenantResult(CreateOwnTenantStatus.Created, Guid.NewGuid(), "rezaei.clinic.example.test"));
        using WebApplication app = CreateApp(sender.Object);

        HttpContext created = await InvokeAsync(
            app, PartiesApiOperations.CreateOwnTenant, HttpMethods.Post,
            "{\"name\":\"Clinic\",\"subdomain\":\"rezaei\",\"category\":\"dermatology\",\"businessDetails\":" + ValidDetails + "}");
        HttpContext invalid = await InvokeAsync(
            app, PartiesApiOperations.CreateOwnTenant, HttpMethods.Post,
            "{\"name\":\"Clinic\",\"subdomain\":\"rezaei\",\"businessDetails\":{\"website\":\"javascript:alert(1)\"}}");

        created.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        captured!.Category.Should().Be("dermatology");
        captured.BusinessDetails!.Phones.Should().Equal("021 1234 5678");
        invalid.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        sender.Verify(item => item.Send(It.IsAny<CreateOwnTenantCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static WebApplication CreateApp(ISender sender, bool onboardingEnabled = true, bool registerProfile = true)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [BaseModule.TenantOnboardingFeatureKey] = onboardingEnabled.ToString(),
        });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy(PartiesRateLimitPolicies.SelfServiceTenantCreation, _ => RateLimitPartition.GetNoLimiter("x"));
            options.AddPolicy(PartiesRateLimitPolicies.PublicLogo, _ => RateLimitPartition.GetNoLimiter("x"));
        });
        builder.Services.AddSingleton(Mock.Of<IPartiesService>());
        builder.Services.AddSingleton(Mock.Of<ISecurityAuditWriter>());
        builder.Services.AddSingleton(sender);
        builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        builder.Services.AddOptions<TenantAddressOptions>().Configure(options => options.TenantBaseDomain = "clinic.example.test");
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns("user-1");
        builder.Services.AddSingleton(requestContext.Object);
        if (registerProfile)
            builder.Services.AddSingleton(new SelfServiceTenantProfile("clinic-app", ["dermatology", "other"]));

        WebApplication app = builder.Build();
        _ = app.MapPartiesApi();
        return app;
    }

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string operationId, string method, string? jsonBody = null)
    {
        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.Request.Method = method;
        if (jsonBody is not null)
        {
            byte[] payload = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(payload);
            context.Request.ContentLength = payload.Length;
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature());
        }
        context.Request.Headers[PartiesApiRoutes.IdempotencyKeyHeader] = "idem-key-1234";
        context.Response.Body = new MemoryStream();
        await FindEndpoint(app, operationId).RequestDelegate!(context);
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string operationId) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

    private sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
