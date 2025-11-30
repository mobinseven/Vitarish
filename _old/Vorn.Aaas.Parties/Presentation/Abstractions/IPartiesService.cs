
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Presentation.Contracts;

namespace Vorn.Aaas.Parties.Presentation.Abstractions;
/// <summary>
/// Contract for working with Owners and Tenants inside the application (no HTTP required).
/// </summary>
public interface IPartiesService
{
    Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default);
    Task<Owner?> CreateOwnerAsync(CreateOwnerRequest request, CancellationToken ct = default);
    Task<Owner?> UpdateOwnerAsync(UpdateOwnerRequest request, CancellationToken ct = default);
    Task DeleteOwnerAsync(Guid id, CancellationToken ct = default);

    Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<Tenant?> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default);
    Task<Tenant?> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken ct = default);
    Task DeleteTenantAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicatorRequest request, CancellationToken ct = default);
    Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
}
