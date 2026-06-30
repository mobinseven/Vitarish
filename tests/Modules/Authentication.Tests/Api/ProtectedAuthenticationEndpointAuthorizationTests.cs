using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Authentication.Tests.Api;

public class ProtectedAuthenticationEndpointAuthorizationTests
{
    public static TheoryData<string> ProtectedEndpointPatterns => new()
    {
        "/api/modules/authentication/session",
        "/api/modules/authentication/me",
        "/api/modules/authentication/logout/context",
        "/api/modules/authentication/logout"
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_use_browser_or_bearer_group_policy(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint endpoint = FindRouteEndpoint(app, routePattern);

        AuthorizeAttribute[] authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToArray();

        authorizeMetadata.Should().ContainSingle();
        authorizeMetadata[0].AuthenticationSchemes.Should().Be(ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_do_not_add_default_only_authorization_metadata(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint endpoint = FindRouteEndpoint(app, routePattern);

        endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().NotContain(attribute => string.IsNullOrWhiteSpace(attribute.AuthenticationSchemes));
    }

    private static WebApplication CreateAppWithProtectedAuthenticationEndpoints()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();
        RouteGroupBuilder protectedGroup = app.MapProtectedModuleGroup(AuthenticationRoutes.BasePath);
        protectedGroup.MapCurrentSessionEndpoints();
        protectedGroup.MapLogoutEndpoints();
        return app;
    }

    private static RouteEndpoint FindRouteEndpoint(WebApplication app, string routePattern)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
                endpoint.RoutePattern.RawText == routePattern ||
                endpoint.RoutePattern.RawText == routePattern[AuthenticationRoutes.BasePath.Length..]);
    }
}
