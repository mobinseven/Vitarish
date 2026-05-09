namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class StoreService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<Store, StoreDescriptor, StoreReadDto, StoreWriteDto, StoreDescriptorDto>
    (
        mediator,
        store => store.ToDto(),
        store => store.ToDomain(),
        store => store.ToDomain()
    ),
    IStoreService
{
    public async Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken)
    {
        Store? store = await dbContext.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return store?.ToDto();
    }
}
