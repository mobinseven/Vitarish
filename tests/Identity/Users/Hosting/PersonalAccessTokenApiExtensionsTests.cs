using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Users.Hosting.Api;

namespace Vitarish.Users.Tests.Hosting;

public class PersonalAccessTokenApiExtensionsTests
{
    [Fact]
    public void MapPersonalAccessTokenEndpoints_RegistersEndpoint()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapPersonalAccessTokenEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText?.TrimEnd('/'), "/api/pat", StringComparison.Ordinal));

        endpoint.Should().NotBeNull();
    }

    [Fact]
    public void MapPersonalAccessTokenEndpoints_AddsAuthorizationMetadata()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        app.MapPersonalAccessTokenEndpoints();

        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText?.TrimEnd('/'), "/api/pat", StringComparison.Ordinal));

        endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().Contain(a => a.AuthenticationSchemes == "IdentityServerAccessToken");
    }
}
