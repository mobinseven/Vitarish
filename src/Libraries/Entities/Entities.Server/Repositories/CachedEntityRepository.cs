namespace Vitarish.Entities.Server.Repositories;
/// <summary>
/// Repository decorator that adds caching layer to entity operations.
/// Implements cache-aside pattern: reads check cache first, writes invalidate cache.
/// Works with EntityInterceptor for automatic cache invalidation on updates/deletes.
/// </summary>
public class CachedEntityRepository<TEntity, TDescriptor>(
    IEntityRepository<TEntity, TDescriptor> innerRepository,
    IEntityCache<TEntity> cache)
    : IEntityRepository<TEntity, TDescriptor>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    /// <summary>
    /// Gets entity by ID using cache-aside pattern.
    /// Checks cache first, falls back to database if not found.
    /// </summary>
    public async Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await cache.GetOrAddAsync(id,
            () => innerRepository.GetAsync(id, cancellationToken));
    }

    /// <summary>
    /// Gets entities by descriptor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No caching for list operations</b> to avoid cache invalidation complexity.
    /// List queries are typically filtered/sorted differently and harder to cache effectively.
    /// </para>
    /// <para>
    /// <b>Performance note:</b> Typical cache hit rates for <see cref="GetAsync(Guid, CancellationToken)"/> (by ID) are 80-90%.
    /// List queries are not cached, so their performance depends entirely on the underlying data store.
    /// </para>
    /// <para>
    /// <b>When to implement custom list caching:</b> If your application has high-volume, repeated list queries with
    /// consistent filtering (e.g., "get all active users", "get recent orders"), consider implementing application-level
    /// caching or query result caching tailored to your specific query patterns and invalidation needs.
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<TEntity>> GetAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return innerRepository.GetAsync(descriptor, cancellationToken);
    }

    /// <summary>
    /// Counts entities by descriptor. No caching for count operations.
    /// </summary>
    public Task<int> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return innerRepository.CountAsync(descriptor, cancellationToken);
    }

    /// <summary>
    /// Adds entity. Cache will be populated by EntityInterceptor on successful save.
    /// </summary>
    public Task<TEntity?> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return innerRepository.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adds multiple entities. Cache will be populated by EntityInterceptor.
    /// </summary>
    public Task<IReadOnlyList<TEntity>> AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return innerRepository.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Adds or updates entity. Cache will be updated by EntityInterceptor.
    /// </summary>
    public Task<TEntity?> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return innerRepository.AddOrUpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates entity. Cache will be updated by EntityInterceptor.
    /// </summary>
    public Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return innerRepository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities. Cache will be updated by EntityInterceptor.
    /// </summary>
    public Task UpdateRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return innerRepository.UpdateRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Removes entity. Cache will be invalidated by EntityInterceptor.
    /// </summary>
    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return innerRepository.RemoveAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Removes entities by descriptor. No cache invalidation needed as list queries aren't cached.
    /// Individual entity caches will be invalidated by EntityInterceptor if entities are loaded.
    /// </summary>
    public Task RemoveAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return innerRepository.RemoveAsync(descriptor, cancellationToken);
    }

    /// <summary>
    /// Removes multiple entities. Cache will be invalidated by EntityInterceptor.
    /// </summary>
    public Task RemoveRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return innerRepository.RemoveRangeAsync(entities, cancellationToken);
    }
}
