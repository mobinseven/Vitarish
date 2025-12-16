namespace Entities.Server.Tests;

public class CrudTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public CrudTests()
    {
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Can_Perform_CRUD_Operations()
    {
        IMediator mediator = _provider.GetRequiredService<IMediator>();
        TestEntity entity = new()
        { Name = "Initial" };

        TestEntity? added = await mediator.Send(new AddEntityCommand<TestEntity, TestDescriptor>(entity));
        Assert.NotNull(added);
        Assert.Equal("Initial", added!.Name);

        TestEntity? fetched = await mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.NotNull(fetched);
        Assert.Equal("Initial", fetched!.Name);

        entity.Name = "Updated";
        TestEntity? updated = await mediator.Send(new UpdateEntityCommand<TestEntity, TestDescriptor>(entity));
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);

        IReadOnlyList<TestEntity> bulk = await mediator.Send(
            new AddEntitiesCommand<TestEntity, TestDescriptor>(
            [
                new() { Name = "Bulk-1" },
                new() { Name = "Bulk-2" }
            ]));
        Assert.Equal(2, bulk.Count);

        IReadOnlyList<TestEntity> all = await mediator.Send(new GetEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(3, all.Count);

        List<TestEntity> toUpdate = all
            .Select(e =>
            {
                e.Name += "-Updated";
                return e;
            })
            .ToList();

        IReadOnlyList<TestEntity> updatedBulk = await mediator.Send(new UpdateEntitiesCommand<TestEntity, TestDescriptor>(toUpdate));
        Assert.All(updatedBulk, e => Assert.EndsWith("-Updated", e.Name));

        int count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(3, count);

        TestEntity bulkEntity = updatedBulk.First(e => e.Id == bulk[0].Id);
        await mediator.Send(new RemoveEntityCommand<TestEntity, TestDescriptor>(bulkEntity));

        count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(2, count);

        await mediator.Send(new RemoveEntityByIdCommand<TestEntity, TestDescriptor>(entity.Id));
        TestEntity? afterIdRemoval = await mediator.Send(new GetEntityByIdQuery<TestEntity, TestDescriptor>(entity.Id));
        Assert.Null(afterIdRemoval);

        count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(1, count);

        await mediator.Send(new RemoveEntitiesByDescriptorCommand<TestEntity, TestDescriptor>(new TestDescriptor()));
        count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(0, count);

        IReadOnlyList<TestEntity> secondBatch = await mediator.Send(
            new AddEntitiesCommand<TestEntity, TestDescriptor>(
            [
                new() { Name = "Second-1" },
                new() { Name = "Second-2" }
            ]));
        Assert.Equal(2, secondBatch.Count);

        await mediator.Send(new RemoveEntitiesCommand<TestEntity, TestDescriptor>(secondBatch));
        count = await mediator.Send(new CountEntitiesQuery<TestEntity, TestDescriptor>(new TestDescriptor()));
        Assert.Equal(0, count);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
