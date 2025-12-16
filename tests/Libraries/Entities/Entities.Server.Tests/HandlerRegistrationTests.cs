namespace Entities.Server.Tests;

public class HandlerRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddEntitiesServerTestsEntityHandlers();
        _ = services.AddSingleton<IEntityRepository<TestEntity, TestDescriptor>, InMemoryRepository>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void GeneratedHandlers_AreRegistered()
    {
        using ServiceProvider provider = BuildProvider();
        Type[] handlerTypes =
        [
            typeof(IRequestHandler<AddEntityCommand<TestEntity, TestDescriptor>, TestEntity?>),
            typeof(IRequestHandler<AddEntitiesCommand<TestEntity, TestDescriptor>, IReadOnlyList<TestEntity>>),
            typeof(IRequestHandler<UpdateEntityCommand<TestEntity, TestDescriptor>, TestEntity?>),
            typeof(IRequestHandler<UpdateEntitiesCommand<TestEntity, TestDescriptor>, IReadOnlyList<TestEntity>>),
            typeof(IRequestHandler<GetEntityByIdQuery<TestEntity, TestDescriptor>, TestEntity?>),
            typeof(IRequestHandler<GetEntitiesQuery<TestEntity, TestDescriptor>, IReadOnlyList<TestEntity>>),
            typeof(IRequestHandler<RemoveEntityCommand<TestEntity, TestDescriptor>>),
            typeof(IRequestHandler<RemoveEntitiesCommand<TestEntity, TestDescriptor>>),
            typeof(IRequestHandler<RemoveEntityByIdCommand<TestEntity, TestDescriptor>>),
            typeof(IRequestHandler<RemoveEntitiesByDescriptorCommand<TestEntity, TestDescriptor>>),
            typeof(IRequestHandler<CountEntitiesQuery<TestEntity, TestDescriptor>, int>)
        ];

        foreach(Type type in handlerTypes)
        {
            object? handler = provider.GetService(type);
            Assert.NotNull(handler);
        }
    }

    [Fact]
    public void CacheInvalidationService_IsRegistered()
    {
        using ServiceProvider provider = BuildProvider();
        IEntityCacheInvalidationService? service = provider.GetService<IEntityCacheInvalidationService>();
        Assert.NotNull(service);
    }
}
