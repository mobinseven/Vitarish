namespace Entities.Server.Tests;

/// <summary>
/// Tests for scalability under concurrent load.
/// These tests verify that the system can handle multiple simultaneous operations
/// without race conditions, deadlocks, or performance degradation.
/// </summary>
public class ScalabilityTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMediator _mediator;

    public ScalabilityTests()
    {
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();
        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Concurrent_Reads_Should_Not_Block()
    {
        // Arrange
        TestEntity entity = new()
        { Name = "ConcurrentTest" };
        _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Act - Perform 100 concurrent reads
        List<Task<TestEntity?>> tasks = [.. Enumerable.Range(0, 100).Select(_ => _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id)))];

        Stopwatch stopwatch = Stopwatch.StartNew();
        TestEntity?[] results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.All(results, Assert.NotNull);
        Assert.All(results, result => Assert.Equal("ConcurrentTest", result!.Name));
        // Should complete in reasonable time (< 1 second for 100 operations)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Concurrent reads took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public async Task Concurrent_Writes_Should_Not_Cause_Conflicts()
    {
        // Arrange - Create 50 entities concurrently
        List<TestEntity> entities = [.. Enumerable.Range(0, 50).Select(i => new TestEntity { Name = $"Entity-{i}" })];

        // Act
        List<Task<TestEntity?>> tasks = [.. entities.Select(e => _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(e)))];

        TestEntity?[] results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, Assert.NotNull);

        // Verify all entities were created
        IReadOnlyList<TestEntity> allEntities = await _mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(50, allEntities.Count);
    }

    [Fact]
    public async Task AddOrUpdate_Under_Concurrent_Load_Should_Be_Safe()
    {
        // Arrange - Same entity ID used by multiple threads
        Guid entityId = Guid.NewGuid();
        int iterations = 20;

        // Act - Multiple threads try to add/update the same entity
        List<Task<TestEntity?>> tasks = [.. Enumerable.Range(0, iterations)
            .Select(i => Task.Run(async () =>
            {
                TestEntity entity = new()
                {
                    Id = entityId,
                    Name = $"Version-{i}"
                };
                return await _mediator.Send(new AddOrUpdateEntityCommand<TestEntity, TestDescriptor>(entity));
            }))];

        TestEntity?[] results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, Assert.NotNull);

        // Verify entity exists with one of the versions
        TestEntity? final = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entityId));
        Assert.NotNull(final);
        Assert.StartsWith("Version-", final!.Name);

        // Verify only one entity was created (no duplicates from race condition)
        IReadOnlyList<TestEntity> all = await _mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        _ = Assert.Single(all.Where(e => e.Id == entityId));
    }

    [Fact]
    public async Task Bulk_Operations_Should_Handle_Large_Collections()
    {
        // Arrange - Create 1000 entities
        List<TestEntity> entities = [.. Enumerable.Range(0, 1000).Select(i => new TestEntity { Name = $"Bulk-{i}" })];

        // Act
        Stopwatch stopwatch = Stopwatch.StartNew();
        IReadOnlyList<TestEntity> results = await _mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(entities));
        stopwatch.Stop();

        // Assert
        Assert.Equal(1000, results.Count);
        // Should complete efficiently (< 2 seconds for 1000 entities)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Bulk add took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");

        // Verify all were created
        int count = await _mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(1000, count);
    }

    [Fact]
    public async Task Mixed_Read_Write_Load_Should_Be_Consistent()
    {
        // Arrange - Add initial entities
        List<TestEntity> initialEntities = [.. Enumerable.Range(0, 20).Select(i => new TestEntity { Name = $"Initial-{i}" })];

        _ = await _mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(initialEntities));

        ConcurrentBag<Exception> errors = [];

        // Act - Mixed concurrent operations (reads, writes, updates)
        IEnumerable<Task> readTasks = Enumerable.Range(0, 50)
            .Select(d => Task.Run(async () =>
            {
                try
                {
                    TestEntity randomEntity = initialEntities[Random.Shared.Next(initialEntities.Count)];
                    _ = await _mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(randomEntity.Id));
                }
                catch(Exception ex)
                {
                    errors.Add(ex);
                }
            }));

        IEnumerable<Task> writeTasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    TestEntity entity = new()
                    { Name = $"New-{i}" };
                    _ = await _mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));
                }
                catch(Exception ex)
                {
                    errors.Add(ex);
                }
            }));

        IEnumerable<Task> updateTasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    TestEntity entity = initialEntities[i];
                    entity.Name = $"Updated-{i}";
                    _ = await _mediator.Send(new UpdateEntityCommand<TestEntity, TestDescriptor>(entity));
                }
                catch(Exception ex)
                {
                    errors.Add(ex);
                }
            }));

        await Task.WhenAll(readTasks.Concat(writeTasks).Concat(updateTasks));

        // Assert
        Assert.Empty(errors); // No exceptions during concurrent operations

        // Verify final state is consistent
        int finalCount = await _mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(30, finalCount); // 20 initial + 10 new
    }

    [Fact]
    public async Task Bulk_Update_Should_Handle_Batching_Correctly()
    {
        // Arrange - Create 1500 entities (should trigger batching at 500)
        List<TestEntity> entities = [.. Enumerable.Range(0, 1500).Select(i => new TestEntity { Name = $"Original-{i}" })];

        _ = await _mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(entities));

        // Act - Update all entities
        foreach(TestEntity entity in entities)
        {
            entity.Name = entity.Name.Replace("Original", "Updated");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        _ = await _mediator.Send(new UpdateEntitiesCommand<TestEntity, TestDescriptor>(entities));
        stopwatch.Stop();

        // Assert
        // Should complete efficiently even with batching (< 3 seconds)
        Assert.True(stopwatch.ElapsedMilliseconds < 3000,
            $"Bulk update took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");

        // Verify all were updated
        IReadOnlyList<TestEntity> updated = await _mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.All(updated, e => Assert.StartsWith("Updated-", e.Name));
    }

    [Fact]
    public async Task Bulk_Remove_Should_Handle_Large_Collections()
    {
        // Arrange - Create 800 entities
        List<TestEntity> entities = [.. Enumerable.Range(0, 800).Select(i => new TestEntity { Name = $"ToRemove-{i}" })];

        _ = await _mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(entities));

        // Act - Remove all in bulk
        Stopwatch stopwatch = Stopwatch.StartNew();
        await _mediator.Send(new RemoveEntitiesCommand<TestEntity, TestDescriptor>(entities));
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Bulk remove took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");

        // Verify all were removed
        int count = await _mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(0, count);
    }

    public void Dispose() => _provider.Dispose();
}
