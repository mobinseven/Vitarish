namespace Vitarish.Entities.Server.Caching;
/// <summary>
/// Distributed cache implementation for entities using IDistributedCache (e.g., Redis).
/// Implements cache-aside pattern with configurable TTL.
/// </summary>
public class EntityDistributedCache<TEntity>(
    IDistributedCache cache,
    DistributedCacheEntryOptions? options = null) : IEntityCache<TEntity>
    where TEntity : Entity
{
    private readonly DistributedCacheEntryOptions _options = options ?? new DistributedCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private static string GetCacheKey(Guid id) => $"{typeof(TEntity).Name}:{id}";

    public TEntity? Get(Guid id)
    {
        string? json = cache.GetString(GetCacheKey(id));
        return json is null ? default : JsonSerializer.Deserialize<TEntity>(json);
    }

    public async Task<TEntity?> GetOrAddAsync(Guid id, Func<Task<TEntity?>> factory)
    {
        string key = GetCacheKey(id);
        string? json = await cache.GetStringAsync(key);

        if (json is not null)
        {
            return JsonSerializer.Deserialize<TEntity>(json);
        }

        TEntity? entity = await factory();
        if (entity is not null)
        {
            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(entity),
                _options);
        }

        return entity;
    }

    public void Set(TEntity entity) => cache.SetString(
            GetCacheKey(entity.Id),
            JsonSerializer.Serialize(entity),
            _options);

    public void Remove(Guid id) => cache.Remove(GetCacheKey(id));
}
