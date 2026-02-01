using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Abstractions;
/// <summary>
/// Contract for working with Owners and Tenants inside the application (no HTTP required).
/// </summary>
public interface IPartiesService
{
    // Owner methods
    Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default);
    Task<Owner?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default);
    Task<Owner?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default);
    Task DeleteOwnerAsync(Guid id, CancellationToken ct = default);

    // Tenant methods
    Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<Tenant?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default);
    Task<Tenant?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default);
    Task DeleteTenantAsync(Guid id, CancellationToken ct = default);

    // TenantIndicator methods
    Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicator request, CancellationToken ct = default);
    Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
    Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default);
}
