using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Api;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Contracts.Routes;
using BaseModule = Vitarish.Modules.Base.Core.Module;

namespace Parties.Tests.Api;

public sealed class SubdomainAvailabilityEndpointTests
{
    [Fact]
    public void Requires_an_authenticated_user_but_not_ambient_tenant_access()
    {
        using WebApplication app = CreateMappedApp(onboardingEnabled: true, baseDomain: "clinic.example.test", Mock.Of<ISender>());
        RouteEndpoint endpoint = FindEndpoint(app);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer)
            .And.NotContain(metadata => metadata.Policy == IdentityDefinitions.TenantAccess);
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route
            .Should().Contain($"{PartiesApiRoutes.Base}{PartiesApiRoutes.Onboarding}{PartiesApiRoutes.SubdomainAvailability}");
    }

    [Theory]
    [InlineData(false, "clinic.example.test")]
    [InlineData(true, null)]
    [InlineData(true, " ")]
    public async Task Answers_not_found_while_onboarding_is_off_or_has_no_base_domain(bool onboardingEnabled, string? baseDomain)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(onboardingEnabled, baseDomain, sender.Object);

        HttpContext context = await InvokeAsync(app, "?subdomain=rezaei");

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?subdomain=")]
    [InlineData("?subdomain=%20")]
    public async Task Rejects_a_missing_subdomain_at_transport(string query)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(onboardingEnabled: true, "clinic.example.test", sender.Object);

        HttpContext context = await InvokeAsync(app, query);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Rejects_a_subdomain_longer_than_one_dns_label_at_transport()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        using WebApplication app = CreateMappedApp(onboardingEnabled: true, "clinic.example.test", sender.Object);

        HttpContext context = await InvokeAsync(app, $"?subdomain={new string('a', 64)}");

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_the_query_answer_when_onboarding_is_available()
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(new GetTenantSubdomainAvailabilityQuery("rezaei"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantSubdomainAvailability("rezaei", "rezaei.clinic.example.test", TenantSubdomainAvailabilityStatus.Taken));
        using WebApplication app = CreateMappedApp(onboardingEnabled: true, "clinic.example.test", sender.Object);

        HttpContext context = await InvokeAsync(app, "?subdomain=rezaei");

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Body.Position = 0;
        string body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("\"status\":\"Taken\"").And.Contain("\"hostname\":\"rezaei.clinic.example.test\"");
    }

    private static WebApplication CreateMappedApp(bool onboardingEnabled, string? baseDomain, ISender sender)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [BaseModule.TenantOnboardingFeatureKey] = onboardingEnabled.ToString(),
        });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<IPartiesService>());
        builder.Services.AddSingleton(sender);
        builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        builder.Services.AddOptions<TenantAddressOptions>().Configure(options => options.TenantBaseDomain = baseDomain);
        builder.Services.AddSingleton(new SelfServiceTenantProfile("clinic-app", ["Clinic"]));
        WebApplication app = builder.Build();
        _ = app.MapPartiesApi();
        return app;
    }

    private static async Task<HttpContext> InvokeAsync(WebApplication app, string query)
    {
        RouteEndpoint endpoint = FindEndpoint(app);
        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString(query);
        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        return context;
    }

    private static RouteEndpoint FindEndpoint(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == PartiesApiOperations.GetSubdomainAvailability);
}
