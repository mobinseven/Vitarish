using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using Vitarish.Accounts.Infrastructure.Extensions;

namespace Vitarish.Accounts.Infrastructure.Tests.Extensions;

public class IdentityComponentsEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public void IdentityComponentsEndpointRouteBuilderExtensions_MapIdentityComponents_RegistersExpectedEndpoints()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        IEndpointRouteBuilder endpointRouteBuilder = app;
        int initialDataSourceCount = endpointRouteBuilder.DataSources.Count;

        _ = app.MapAdditionalIdentityEndpoints();

        endpointRouteBuilder.DataSources.Count.Should().BeGreaterThan(initialDataSourceCount);
    }
}
