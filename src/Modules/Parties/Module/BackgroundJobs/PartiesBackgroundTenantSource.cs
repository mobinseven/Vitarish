using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Parties.Module.BackgroundJobs;

/// <summary>
/// Adapts parties tenants to the base background tenant contract.
/// </summary>
public sealed class PartiesBackgroundTenantSource(IPartiesService partiesService) : IBackgroundTenantSource
{
    /// <inheritdoc />
    public async Task<IEnumerable<BackgroundTenant>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<TenantReadDto> tenants = await partiesService.GetTenantsAsync(cancellationToken)
            ?? Array.Empty<TenantReadDto>();
        return tenants.Select(tenant => new BackgroundTenant(tenant.Id, tenant.OwnerId, tenant.AppId));
    }
}
