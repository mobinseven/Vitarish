namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class StoreService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<Store, StoreDescriptor, StoreReadDto, StoreWriteDto, StoreDescriptorDto>
    (
        mediator,
        store => store.ToRead(),
        store => store.ToEntity(),
        store => store.ToDescriptor()
    ),
    IStoreService
{
    public async Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken)
    {
        Store? store = await dbContext.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return store?.ToRead();
    }
}
