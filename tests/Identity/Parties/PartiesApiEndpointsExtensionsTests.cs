namespace Vitarish.Parties.Tests;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Vitarish.Parties.Hosting.Api;

public class PartiesApiEndpointsExtensionsTests
{
    [Fact]
    public void MapOwnerEndpoints_RegistersExpectedNamedRoutes()
    {
        var app = CreateApp();

        app.MapOwnerEndpoints();

        var endpointNames = GetEndpointNames(app);

        Assert.Contains("GetOwner", endpointNames);
        Assert.Contains("GetOwners", endpointNames);
        Assert.Contains("CreateOwner", endpointNames);
        Assert.Contains("EditOwner", endpointNames);
        Assert.Contains("RemoveOwner", endpointNames);
    }

    [Fact]
    public void MapTenantEndpoints_RegistersExpectedNamedRoutes()
    {
        var app = CreateApp();

        app.MapTenantEndpoints();

        var endpointNames = GetEndpointNames(app);

        Assert.Contains("GetTenant", endpointNames);
        Assert.Contains("GetTenants", endpointNames);
        Assert.Contains("GetTenantsByOwner", endpointNames);
        Assert.Contains("GetTenantsByClientId", endpointNames);
        Assert.Contains("CreateTenant", endpointNames);
        Assert.Contains("EditTenant", endpointNames);
        Assert.Contains("RemoveTenant", endpointNames);
    }

    [Fact]
    public void MapTenantIndicatorEndpoints_RegistersExpectedNamedRoutes()
    {
        var app = CreateApp();

        app.MapTenantIndicatorEndpoints();

        var endpointNames = GetEndpointNames(app);

        Assert.Contains("GetTenantIndicators", endpointNames);
        Assert.Contains("AddTenantIndicator", endpointNames);
        Assert.Contains("VerifyTenantIndicator", endpointNames);
        Assert.Contains("ActivateTenantIndicator", endpointNames);
        Assert.Contains("DisableTenantIndicator", endpointNames);
        Assert.Contains("DeleteTenantIndicator", endpointNames);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        return builder.Build();
    }

    private static List<string> GetEndpointNames(IEndpointRouteBuilder app) => app.DataSources
        .SelectMany(source => source.Endpoints)
        .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => name!)
        .ToList();
}
