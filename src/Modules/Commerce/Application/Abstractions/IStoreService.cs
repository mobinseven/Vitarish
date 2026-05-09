using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface IStoreService : IEntityService<StoreReadDto, StoreWriteDto, StoreDescriptorDto>
{
    Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken);
}
