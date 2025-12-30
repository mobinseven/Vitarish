using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Users.Hosting.Api;

namespace Vitarish.Users.Tests.Hosting;

public class UserProfileApiMappingExtensionTests
{
    [Fact]
    public void MapUserProfileEndpoints_RegistersEndpoints()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapUserProfileEndpoints();

        List<RouteEndpoint> endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/api/user/profile/{userId}");
        endpoints.Count(e => e.RoutePattern.RawText == "/api/user/profile/{userId}").Should().Be(2);
    }

    [Fact]
    public void MapUserProfileEndpoints_AddsAuthorizationMetadata()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapUserProfileEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .First(e => e.RoutePattern.RawText == "/api/user/profile/{userId}");

        endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().Contain(a => a.AuthenticationSchemes == "IdentityServerAccessToken");
    }
}
