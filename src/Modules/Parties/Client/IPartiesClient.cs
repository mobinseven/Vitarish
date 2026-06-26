using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;
namespace Vitarish.Parties.Client;
public interface IPartiesClient
{
    Task<PartiesResult<IReadOnlyList<UserTenantDto>>> GetUserTenantsAsync(CancellationToken cancellationToken = default);
    Task<PartiesResult<OwnerReadDto>> GetOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<PartiesResult<IReadOnlyList<OwnerReadDto>>> GetOwnersAsync(CancellationToken cancellationToken = default);
    Task<PartiesResult<OwnerReadDto>> CreateOwnerAsync(OwnerWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<OwnerReadDto>> UpdateOwnerAsync(OwnerWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<bool>> DeleteOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantReadDto>> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PartiesResult<IReadOnlyList<TenantReadDto>>> GetTenantsAsync(CancellationToken cancellationToken = default);
    Task<PartiesResult<IReadOnlyList<TenantReadDto>>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantReadDto>> CreateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantReadDto>> UpdateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<bool>> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantIndicatorReadDto>> GetTenantIndicatorAsync(Guid tenantIndicatorId, CancellationToken cancellationToken = default);
    Task<PartiesResult<IReadOnlyList<TenantIndicatorReadDto>>> GetTenantIndicatorsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantIndicatorReadDto>> CreateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantIndicatorReadDto>> UpdateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<bool>> DeleteTenantIndicatorAsync(Guid tenantIndicatorId, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantReadDto>> SetupTenantAsync(SetupTenantRequest request, CancellationToken cancellationToken = default);
}
