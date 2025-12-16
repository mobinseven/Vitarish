using System.Collections.Concurrent;

namespace Entities.Server.Tests;

public class InMemoryRepository : IEntityRepository<TestEntity, TestDescriptor>
{
    private readonly ConcurrentDictionary<Guid, TestEntity> _store = new();

    public Task<TestEntity?> AddAsync(TestEntity entity, CancellationToken cancellationToken)
    {
        _store[entity.Id] = entity;
        return Task.FromResult<TestEntity?>(entity);
    }

    public Task<IReadOnlyList<TestEntity>> AddRangeAsync(IReadOnlyList<TestEntity> entities, CancellationToken cancellationToken)
    {
        foreach(TestEntity e in entities)
        {
            _store[e.Id] = e;
        }
        return Task.FromResult<IReadOnlyList<TestEntity>>(entities);
    }

    public Task<TestEntity?> AddOrUpdateAsync(TestEntity entity, CancellationToken cancellationToken)
    {
        _store[entity.Id] = entity;
        return Task.FromResult<TestEntity?>(entity);
    }

    public Task<int> CountAsync(TestDescriptor descriptor, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Count);
    }

    public Task<IReadOnlyList<TestEntity>> GetAsync(TestDescriptor descriptor, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<TestEntity>>(_store.Values.ToList());
    }

    public Task<TestEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.TryGetValue(id, out TestEntity? entity) ? entity : null);
    }

    public Task RemoveAsync(TestEntity entity, CancellationToken cancellationToken)
    {
        _ = _store.TryRemove(entity.Id, out _);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TestDescriptor descriptor, CancellationToken cancellationToken)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IReadOnlyList<TestEntity> entities, CancellationToken cancellationToken)
    {
        foreach(TestEntity e in entities)
        {
            _ = _store.TryRemove(e.Id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<TestEntity?> UpdateAsync(TestEntity entity, CancellationToken cancellationToken)
    {
        _store[entity.Id] = entity;
        return Task.FromResult<TestEntity?>(entity);
    }

    public Task UpdateRangeAsync(IReadOnlyList<TestEntity> entities, CancellationToken cancellationToken)
    {
        foreach(TestEntity e in entities)
        {
            _store[e.Id] = e;
        }
        return Task.CompletedTask;
    }
}
