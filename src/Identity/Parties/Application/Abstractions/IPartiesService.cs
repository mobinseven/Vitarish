using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Parties.Application.Abstractions;
/// <summary>
/// Contract for working with Owners and Tenants inside the application (no HTTP required).
/// </summary>
public interface IPartiesService
{
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
}
