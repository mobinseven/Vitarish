using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Api;
using Vitarish.Parties.Contracts.Routes;

namespace Parties.Tests.Api;

public sealed class PartiesEndpointAuthorizationTests
{
    [Fact]
    public void Platform_tenant_selector_is_browser_or_bearer_and_platform_admin_protected()
    {
        using WebApplication app = CreateMappedApp();
        RouteEndpoint endpoint = FindPlatformTenantEndpoint(app);

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("GET");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should()
            .Contain(metadata => metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer)
            .And.Contain(metadata => metadata.Policy == IdentityDefinitions.PlatformAdmin);
        endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>()!.Scheme
            .Should().Be(ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route
            .Should().Contain($"{PartiesApiRoutes.Base}{PartiesApiRoutes.Platform}{PartiesApiRoutes.PlatformTenants}");
    }

    [Fact]
    public void Platform_tenant_selector_does_not_require_ambient_tenant_access()
    {
        using WebApplication app = CreateMappedApp();
        RouteEndpoint endpoint = FindPlatformTenantEndpoint(app);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().NotContain(metadata => metadata.Policy == IdentityDefinitions.TenantAccess);
    }

    private static WebApplication CreateMappedApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<IPartiesService>());
        WebApplication app = builder.Build();
        _ = app.MapPartiesApi();
        return app;
    }

    private static RouteEndpoint FindPlatformTenantEndpoint(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == PartiesApiOperations.PlatformGetTenants);
}
