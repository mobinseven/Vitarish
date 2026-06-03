using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Infrastructure.ViewModels;

namespace Vitarish.Parties.Application.Abstractions;
/// <summary>
/// Contract for working with Owners and Tenants inside the application (no HTTP required).
/// </summary>
public interface IPartiesService
{
    /// <summary>
    /// Retrieves all tenants for a given user by their tenant IDs in a single database query.
    /// Each result includes the tenant details and its primary indicator.
    /// </summary>
    /// <param name="tenantIds">Collection of tenant IDs to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of user tenant views.</returns>
    Task<IEnumerable<UserTenantView>> GetUserTenantsAsync(IEnumerable<Guid> tenantIds, CancellationToken ct = default);
    // Owner methods
    Task<OwnerReadDto?> GetOwnerAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<OwnerReadDto>> GetOwnersAsync(CancellationToken ct = default);
    Task<OwnerReadDto?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default);
    Task<OwnerReadDto?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default);
    Task DeleteOwnerAsync(Guid id, CancellationToken ct = default);

    // Tenant methods
    Task<TenantReadDto?> GetTenantAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<TenantReadDto>> GetTenantsAsync(CancellationToken ct = default);
    Task<IEnumerable<TenantReadDto>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<TenantReadDto?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default);
    Task<TenantReadDto?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default);
    Task DeleteTenantAsync(Guid id, CancellationToken ct = default);

    // TenantIndicator methods
    Task<TenantIndicatorReadDto?> GetTenantIndicatorAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<TenantIndicatorReadDto>> GetTenantIndicatorsByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantIndicatorReadDto?> CreateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default);
    Task<TenantIndicatorReadDto?> UpdateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default);
    Task DeleteTenantIndicatorAsync(Guid id, CancellationToken ct = default);
    Task CreateTenantRolesAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Full self-service onboarding: creates Owner, Tenant, TenantIndicator,
    /// tenant roles, and assigns the caller to TenantOwner / TenantAdmin / TenantUser.
    /// </summary>
    Task<TenantReadDto?> SetupTenantAsync(SetupTenantRequest request, CancellationToken ct = default);
}
