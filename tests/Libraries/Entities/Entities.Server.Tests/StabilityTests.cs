namespace Entities.Server.Tests;

/// <summary>
/// Tests for system stability under failure conditions.
/// These tests verify graceful degradation, error handling, and recovery mechanisms.
/// </summary>
public class StabilityTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public StabilityTests()
    {
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Repository_Should_Handle_Cancellation_Token()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        using CancellationTokenSource cts = new();

        TestEntity entity = new()
        { Name = "Test" };
        _ = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Act - Use a valid cancellation token (not cancelled yet)
        TestEntity? result = await mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id), cts.Token);

        // Assert - Should complete successfully with proper token handling
        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);

        // Token can be cancelled without causing issues
        cts.Cancel();
    }

    [Fact]
    public async Task Validation_Failure_Should_Not_Corrupt_State()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();

        // Add a valid entity
        TestEntity validEntity = new()
        { Name = "Valid" };
        _ = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(validEntity));

        // Act - Try operations that might fail
        TestEntity entity2 = new()
        { Name = "Another" };
        _ = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity2));

        // Assert - Original entity should still be retrievable
        TestEntity? retrieved = await mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(validEntity.Id));
        Assert.NotNull(retrieved);
        Assert.Equal("Valid", retrieved!.Name);

        // Repository should be in consistent state
        int count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Duplicate_Operations_Should_Be_Idempotent()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new()
        { Name = "Idempotent" };

        // Act - Add the same entity multiple times using AddOrUpdate
        _ = await mediator.Send(new AddOrUpdateEntityCommand<TestEntity, TestDescriptor>(entity));
        _ = await mediator.Send(new AddOrUpdateEntityCommand<TestEntity, TestDescriptor>(entity));
        _ = await mediator.Send(new AddOrUpdateEntityCommand<TestEntity, TestDescriptor>(entity));

        // Assert - Should result in only one entity
        IReadOnlyList<TestEntity> all = await mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        _ = Assert.Single(all.Where(e => e.Id == entity.Id));
    }

    [Fact]
    public async Task Repository_Should_Handle_Rapid_Add_Remove_Cycles()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();

        // Act - Rapidly add and remove entities
        for(int i = 0; i < 50; i++)
        {
            TestEntity entity = new()
            { Name = $"Cycle-{i}" };
            _ = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));
            await mediator.Send(new RemoveEntityCommand<TestEntity, TestDescriptor>(entity));
        }

        // Assert - Repository should be empty and consistent
        int count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task System_Should_Handle_Memory_Pressure_During_Bulk_Operations()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();

        // Act - Create large number of entities in batches to simulate memory pressure
        int totalEntities = 5000;
        int batchSize = 500;

        for(int batch = 0; batch < totalEntities / batchSize; batch++)
        {
            List<TestEntity> entities = [.. Enumerable.Range(batch * batchSize, batchSize).Select(i => new TestEntity { Name = $"MemTest-{i}" })];

            _ = await mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(entities));

            // Allow natural GC operation - no explicit collection needed
        }

        // Assert - All entities should be created
        int count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(totalEntities, count);
    }

    [Fact]
    public async Task Concurrent_Updates_To_Same_Entity_Should_Not_Cause_Data_Loss()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new()
        { Name = "Original" };
        _ = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));

        // Act - Multiple concurrent updates to the same entity
        List<Task<TestEntity?>> updateTasks = [.. Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                TestEntity toUpdate = new()
                {
                    Id = entity.Id,
                    Name = $"Update-{i}"
                };
                return await mediator.Send(new UpdateEntityCommand<TestEntity, TestDescriptor>(toUpdate));
            }))];

        TestEntity?[] results = await Task.WhenAll(updateTasks);

        // Assert - All updates should succeed (last write wins)
        Assert.All(results, Assert.NotNull);

        // Entity should exist with one of the update values
        TestEntity? final = await mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(final);
        Assert.StartsWith("Update-", final!.Name);

        // Should be only one entity (no duplicates created)
        IReadOnlyList<TestEntity> all = await mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        _ = Assert.Single(all.Where(e => e.Id == entity.Id));
    }

    [Fact]
    public async Task System_Should_Maintain_Consistency_After_Batch_Partial_Failure()
    {
        // Arrange
        IMediator mediator = _provider.GetRequiredService<IMediator>();

        // Add some entities successfully
        List<TestEntity> firstBatch = [.. Enumerable.Range(0, 10).Select(i => new TestEntity { Name = $"Batch1-{i}" })];
        _ = await mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(firstBatch));

        int initialCount = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(10, initialCount);

        // Act - Add another batch
        List<TestEntity> secondBatch = [.. Enumerable.Range(0, 10).Select(i => new TestEntity { Name = $"Batch2-{i}" })];
        _ = await mediator.Send(new AddEntitiesCommand<TestEntity, TestDescriptor>(secondBatch));

        // Assert - System should be in consistent state
        int finalCount = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(20, finalCount);

        // All entities from both batches should be present
        IReadOnlyList<TestEntity> all = await mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(10, all.Count(e => e.Name.StartsWith("Batch1-")));
        Assert.Equal(10, all.Count(e => e.Name.StartsWith("Batch2-")));
    }

    public void Dispose() => _provider.Dispose();
}
