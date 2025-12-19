using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.Main.Infrastructure.IdentityServer;

namespace Vitarish.Main.Services;

public class IdentityResourcesStore(ApiClientDbContext apiClients) : IResourceStore
{
    async Task<IEnumerable<ApiResource>> IResourceStore.FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        List<ApiClient> apiclients = await apiClients.ApiClients.ToListAsync();
        List<string> scopes = [.. apiclients.Select(a => a.Scopes)];
        return [.. scopes.Where(scope => apiResourceNames.Any(resource => resource == scope)).Select(s => new ApiResource(s)
        {
            Scopes = { s }
        })];
    }

    async Task<IEnumerable<ApiResource>> IResourceStore.FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        List<ApiClient> apiclients = await apiClients.ApiClients.ToListAsync();
        return [.. apiclients.Select(a => a.Scopes).Where(apiscope => scopeNames.Any(r => r == apiscope)).Select(s => new ApiResource(s)
        {
            Scopes = { s }
        })];
    }

    async Task<IEnumerable<ApiScope>> IResourceStore.FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        List<ApiClient> apiclients = await apiClients.ApiClients.ToListAsync();
        List<string> scopes = [.. apiclients.Select(a => a.Scopes)];
        return [.. scopes.Where(apiscope => scopeNames.Any(scope => scope == apiscope)).Select(s => new ApiScope(s))];
    }

    Task<IEnumerable<IdentityResource>> IResourceStore.FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames) => Task.FromResult(IdentityDefinitions.IdentityResources.Where(ir => scopeNames.Any(scope => scope == ir.Name)).Select(ir => ir));

    async Task<Resources> IResourceStore.GetAllResourcesAsync()
    {
        List<ApiClient> apiclients = await apiClients.ApiClients.ToListAsync();
        List<ApiResource> resources = [.. apiclients.Select(a => a.Scopes).Select(s => new ApiResource(s) { Scopes = { s } })];
        List<ApiScope> scopes = [.. apiclients.Select(a => a.Scopes).Select(s => new ApiScope(s))];
        return new Resources(IdentityDefinitions.IdentityResources, resources, scopes);
    }
}
