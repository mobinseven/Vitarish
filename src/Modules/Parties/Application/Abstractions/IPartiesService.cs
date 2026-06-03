using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Parties.Application.Abstractions;
/// <summary>
/// Contract for working with Owners and Tenants inside the application (no HTTP required).
/// </summary>
public interface IPartiesService
{
    /// <summary>
    /// Retrieves all tenants for the current user from the cross-schema view.
    /// Uses the current user context (app.user_id) set via RequestContextInterceptor.
    /// Each result includes the tenant details and its primary indicator.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of user tenants.</returns>
    Task<IEnumerable<UserTenant>> GetUserTenantsAsync(CancellationToken ct = default);
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
