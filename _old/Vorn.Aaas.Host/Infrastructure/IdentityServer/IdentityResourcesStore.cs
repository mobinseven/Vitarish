using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

namespace Vorn.Aaas.Host.Infrastructure.IdentityServer;

public class IdentityResourcesStore(ApiClientDbContext apiClients) : IResourceStore
{
    async Task<IEnumerable<ApiResource>> IResourceStore.FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        var apiclients = await apiClients.ApiClients.ToListAsync();
        var scopes = apiclients.Select(a => a.Scopes).ToList();
        return scopes.Where(scope => apiResourceNames.Any(resource => resource == scope)).Select(s => new ApiResource(s)
        {
            Scopes = { s }
        }).ToList();
    }

    async Task<IEnumerable<ApiResource>> IResourceStore.FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        var apiclients = await apiClients.ApiClients.ToListAsync();
        return apiclients.Select(a => a.Scopes).Where(apiscope => scopeNames.Any(r => r == apiscope)).Select(s => new ApiResource(s)
        {
            Scopes = { s }
        }).ToList();
    }

    async Task<IEnumerable<ApiScope>> IResourceStore.FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        var apiclients = await apiClients.ApiClients.ToListAsync();
        var scopes = apiclients.Select(a => a.Scopes).ToList();
        return scopes.Where(apiscope => scopeNames.Any(scope => scope == apiscope)).Select(s => new ApiScope(s)).ToList();
    }

    Task<IEnumerable<IdentityResource>> IResourceStore.FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames) => Task.FromResult(IdentityDefinitions.IdentityResources.Where(ir => scopeNames.Any(scope => scope == ir.Name)).Select(ir => ir));

    async Task<Resources> IResourceStore.GetAllResourcesAsync()
    {
        var apiclients = await apiClients.ApiClients.ToListAsync();
        var resources = apiclients.Select(a => a.Scopes).Select(s => new ApiResource(s) { Scopes = { s } }).ToList();
        var scopes = apiclients.Select(a => a.Scopes).Select(s => new ApiScope(s)).ToList();
        return new Resources(IdentityDefinitions.IdentityResources, resources, scopes);
    }
}
