namespace Entities.Server.Tests;

public class CacheTests
{
    [Fact]
    public async Task Cache_Is_Evicted_On_Delete_And_Factory_Not_Run_When_Warm()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        EntityMemoryCache<TestEntity> cache = new(memoryCache);
        InMemoryRepository repository = new();
        EntityCacheInvalidator<TestEntity> invalidator = new(cache);

        TestEntity entity = new()
        { Name = "Cached" };
        _ = await repository.AddAsync(entity, CancellationToken.None);

        // Warm on create
        invalidator.Invalidate(
            entity,
            Microsoft.EntityFrameworkCore.EntityState.Added);

        int calls = 0;
        TestEntity? fromCache = await cache.GetOrAddAsync(entity.Id, () =>
        {
            calls++;
            return repository.GetAsync(entity.Id, CancellationToken.None);
        });
        Assert.Equal(0, calls);
        Assert.NotNull(fromCache); // warm

        // Evict on delete + delete from store
        invalidator.Invalidate(
            entity,
            Microsoft.EntityFrameworkCore.EntityState.Deleted);
        await repository.RemoveAsync(entity, CancellationToken.None);

        Assert.Null(cache.Get(entity.Id));
        fromCache = await cache.GetOrAddAsync(entity.Id, () =>
        {
            calls++;
            return repository.GetAsync(entity.Id, CancellationToken.None);
        });
        Assert.Equal(1, calls);      // factory ran
        Assert.Null(fromCache);       // store no longer has it
    }
}
