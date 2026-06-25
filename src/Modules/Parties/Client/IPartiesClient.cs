using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;
namespace Vitarish.Parties.Client;
public interface IPartiesClient
{
    Task<PartiesResult<TenantReadDto>> CreateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantReadDto>> UpdateTenantAsync(TenantWriteDto request, CancellationToken cancellationToken = default);
    Task<PartiesResult<TenantIndicatorReadDto>> CreateTenantIndicatorAsync(TenantIndicatorWriteDto request, CancellationToken cancellationToken = default);
}
