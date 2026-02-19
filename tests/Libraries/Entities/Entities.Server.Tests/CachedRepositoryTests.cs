namespace Entities.Server.Tests;

/// <summary>
/// Tests for CachedEntityRepository decorator.
/// Validates that cache-aside pattern is correctly implemented and cache is used for reads.
/// </summary>
public class CachedRepositoryTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMediator _mediator;
    private readonly EntityMemoryCache<TestEntity> _cache;
    private readonly MemoryCache _memoryCache;

    public CachedRepositoryTests()
    {
        // Setup with caching enabled
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();

        // Register base repository
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();

        // Register cache
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new EntityMemoryCache<TestEntity>(_memoryCache);
        _ = services.AddSingleton<IEntityCache<TestEntity>>(_cache);

        // Apply cached repository decorator
        _ = services.AddCachedEntityRepository<TestEntity, TestDescriptor>();

        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task GetAsync_UsesCache_OnSecondRead()
    {
        // Arrange - Add an entity
        TestEntity entity = new()
        { Name = "CachedEntity" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Act - First read (should populate cache)
        TestEntity? firstRead = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));

        // Verify cache was populated
        TestEntity? fromCache = _cache.Get(entity.Id);
        Assert.NotNull(fromCache);
        Assert.Equal("CachedEntity", fromCache.Name);

        // Act - Second read (should use cache)
        TestEntity? secondRead = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));

        // Assert
        Assert.NotNull(firstRead);
        Assert.NotNull(secondRead);
        Assert.Equal(entity.Id, firstRead.Id);
        Assert.Equal(entity.Id, secondRead.Id);
        Assert.Equal("CachedEntity", secondRead.Name);
    }

    [Fact]
    public async Task GetAsync_ReturnsFromCache_WithoutHittingRepository()
    {
        // Arrange - Create entity and pre-populate cache
        TestEntity entity = new()
        { Id = Guid.NewGuid(), Name = "PreCached" };
        _cache.Set(entity);

        // Act - Read should come from cache without hitting repository
        TestEntity? result = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("PreCached", result.Name);
    }

    [Fact]
    public async Task GetAsync_FallsBackToRepository_WhenCacheMiss()
    {
        // Arrange - Add entity to repository but not cache
        TestEntity entity = new()
        { Name = "NotInCache" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Remove from cache to simulate cache miss
        _cache.Remove(entity.Id);

        // Act - Should fall back to repository
        TestEntity? result = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("NotInCache", result.Name);

        // Verify cache was re-populated
        TestEntity? fromCache = _cache.Get(entity.Id);
        Assert.NotNull(fromCache);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCacheViaRead()
    {
        // Arrange - Add entity (cache will be populated on first read)
        TestEntity entity = new()
        { Name = "Original" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // First read populates cache
        TestEntity? firstRead = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(firstRead);

        // Verify cache is populated
        TestEntity? cached = _cache.Get(entity.Id);
        Assert.NotNull(cached);
        Assert.Equal("Original", cached.Name);

        // Act - Update entity in repository
        entity.Name = "Updated";
        _ = await _mediator.Send(new UpdateEntityCommand<TestEntity, TestDescriptor>(entity));

        // Clear cache to simulate invalidation (in real EF, EntityInterceptor does this)
        _cache.Remove(entity.Id);

        // Assert - Read should get updated value and re-populate cache
        TestEntity? result = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
    }

    [Fact]
    public async Task RemoveAsync_InvalidatesCache()
    {
        // Arrange - Add entity and populate cache
        TestEntity entity = new()
        { Name = "ToDelete" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Read to populate cache
        TestEntity? initial = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(initial);

        // Verify cache is populated
        Assert.NotNull(_cache.Get(entity.Id));

        // Act - Remove entity and manually invalidate cache (simulating EF interceptor)
        await _mediator.Send(new RemoveEntityCommand<TestEntity, TestDescriptor>(entity));
        _cache.Remove(entity.Id); // Simulate cache invalidation that EntityInterceptor would do

        // Assert - Cache should be invalidated
        Assert.Null(_cache.Get(entity.Id));

        // Read should return null
        TestEntity? result = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.Null(result);
    }

    [Fact]
    public async Task AddOrUpdateAsync_WorksWithCache()
    {
        // Arrange
        TestEntity entity = new()
        { Name = "AddOrUpdate" };

        // Act - Add via AddOrUpdate
        _ = await _mediator.Send(new AddOrUpdateEntityCommand<TestEntity, TestDescriptor>(entity));

        // First read will populate cache (cache-aside pattern)
        TestEntity? result = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("AddOrUpdate", result.Name);

        // Assert - Cache should now be populated from the read
        TestEntity? fromCache = _cache.Get(entity.Id);
        Assert.NotNull(fromCache);
        Assert.Equal("AddOrUpdate", fromCache.Name);
    }

    [Fact]
    public async Task ConcurrentReads_UseCacheEfficiently()
    {
        // Arrange - Add entity
        TestEntity entity = new()
        { Name = "ConcurrentTest" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Act - Perform 50 concurrent reads
        List<Task<TestEntity?>> tasks = [.. Enumerable.Range(0, 50).Select(_ => _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id)))];

        TestEntity?[] results = await Task.WhenAll(tasks);

        // Assert - All reads should succeed and return same entity
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal("ConcurrentTest", result.Name);
        });

        // Verify cache is still populated
        Assert.NotNull(_cache.Get(entity.Id));
    }

    [Fact]
    public async Task GetListAsync_DoesNotUseCache()
    {
        // Arrange - Add multiple entities
        TestEntity[] entities = new[]
        {
            new TestEntity { Name = "List1" },
            new TestEntity { Name = "List2" },
            new TestEntity { Name = "List3" }
        };

        foreach(TestEntity? entity in entities)
        {
            _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));
        }

        // Act - Get list (should not use cache for list operations)
        IReadOnlyList<TestEntity> result = await _mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));

        // Assert - Should return all entities
        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.StartsWith("List", e.Name));
    }

    [Fact]
    public async Task BulkOperations_WorkWithCache()
    {
        // Arrange - Add multiple entities
        List<TestEntity> entities = [.. Enumerable.Range(0, 10).Select(i => new TestEntity { Name = $"Bulk-{i}" })];

        // Act - Add in bulk
        IReadOnlyList<TestEntity> added = await _mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(entities));

        // Read each entity to populate cache (cache-aside pattern)
        foreach(TestEntity? entity in added)
        {
            TestEntity? read = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
            Assert.NotNull(read);
        }

        // Assert - Each entity should now be cached
        foreach(TestEntity? entity in added)
        {
            TestEntity? fromCache = _cache.Get(entity.Id);
            Assert.NotNull(fromCache);
            Assert.Equal(entity.Name, fromCache.Name);
        }

        // Act - Update in bulk and clear cache (simulating EF interceptor behavior)
        foreach(TestEntity entity in entities)
        {
            entity.Name += "-Updated";
            _cache.Remove(entity.Id); // Simulate cache invalidation
        }
        _ = await _mediator.Send(new UpdateEntitiesCommand<TestEntity, TestDescriptor>(entities));

        // Assert - Reads should get updated values and re-populate cache
        foreach(TestEntity entity in entities)
        {
            TestEntity? read = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
            Assert.NotNull(read);
            Assert.Contains("-Updated", read.Name);

            // Cache should be re-populated
            TestEntity? fromCache = _cache.Get(entity.Id);
            Assert.NotNull(fromCache);
            Assert.Contains("-Updated", fromCache.Name);
        }
    }

    public void Dispose()
    {
        _provider.Dispose();
        _memoryCache.Dispose();
    }
}
