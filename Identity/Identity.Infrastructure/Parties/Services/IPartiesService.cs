using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Parties.Services;

public interface IPartiesService
{
    Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default);
    Task<Result<Owner>> CreateOwnerAsync(CreateOwner command, CancellationToken ct = default);
    Task<Result> UpdateOwnerAsync(UpdateOwner command, CancellationToken ct = default);
    Task<Result> DeleteOwnerAsync(Guid id, CancellationToken ct = default);

    Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<Result<Tenant>> CreateTenantAsync(CreateTenant command, CancellationToken ct = default);
    Task<Result> UpdateTenantAsync(UpdateTenant command, CancellationToken ct = default);
    Task<Result> DeleteTenantAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<TenantIndicator>> AddTenantIndicatorAsync(AddTenantIndicator command, CancellationToken ct = default);
    Task<Result> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<Result> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<Result> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<Result> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
}
