using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Users.Hosting.Api;

namespace Vitarish.Users.Tests.Hosting;

public class UserAccessApiMappingExtensionTests
{
    [Fact]
    public void MapUserAccessEndpoints_RegistersEndpoints()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapUserAccessEndpoints();

        List<RouteEndpoint> endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/users/role");
        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/users/checkrole");
        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/users/current");
    }

    [Fact]
    public void MapUserAccessEndpoints_AddsAuthorizationMetadata()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapUserAccessEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/api/users/current");

        endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().Contain(a => a.AuthenticationSchemes == "IdentityServerAccessToken");
    }
}
