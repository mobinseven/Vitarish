namespace Vitarish.Entities.Server.Caching;

public class EntityMemoryCache<TEntity> : IEntityCache<TEntity>
    where TEntity : Entity
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _options;

    public EntityMemoryCache(IMemoryCache cache, MemoryCacheEntryOptions? options = null)
    {
        _cache = cache;
        _options = options ?? new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
    }

    public TEntity? Get(Guid id)
    {
        return _cache.TryGetValue(id, out TEntity? value) ? value : default;
    }

    public async Task<TEntity?> GetOrAddAsync(Guid id, Func<Task<TEntity?>> factory)
    {
        if(_cache.TryGetValue(id, out TEntity? value))
        {
            return value;
        }

        TEntity? entity = await factory();
        if(entity is not null)
        {
            Set(entity);
        }
        return entity;
    }

    public void Set(TEntity entity)
    {
        _ = _cache.Set(entity.Id, entity, _options);
    }

    public void Remove(Guid id)
    {
        _cache.Remove(id);
    }
}
