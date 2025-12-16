using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Entities.Server.Repositories;

public abstract class EntityRepository<TEntity, TDescriptor, TDbContext>(TDbContext dbContext)
    : IEntityRepository<TEntity, TDescriptor>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    protected virtual IQueryable<TEntity> ApplyCore(IQueryable<TEntity> query, TDescriptor descriptor)
    {
        query = Apply(query, descriptor);

        if(query is not IOrderedQueryable<TEntity>)
        {
            query = query.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt);
        }

        return query.ApplyPagination(descriptor);
    }

    protected virtual IQueryable<TEntity> Apply(IQueryable<TEntity> query, TDescriptor descriptor)
    {
        return query.ApplyDescription(descriptor);
    }

    public Task<int> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return ApplyCore(Set.AsNoTracking(), descriptor).CountAsync(cancellationToken);
    }

    public Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Set.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return await ApplyCore(Set.AsNoTracking(), descriptor).ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        EntityEntry<TEntity> entry = await Set.AddAsync(entity, cancellationToken);
        await SaveChangesWithHandlingAsync(cancellationToken);
        DbContext.Entry(entity).State = EntityState.Detached;
        return entry.Entity;
    }

    /// <summary>
    /// Batch size for bulk operations. Can be overridden in derived classes to customize based on specific database or entity requirements.
    /// Default is 500. For entities with many relationships or large payloads, consider smaller batch sizes (e.g., 100-200).
    /// </summary>
    /// <remarks>
    /// To customize per instance, use an init-only setter in derived classes:
    /// <code>
    /// public class CustomerRepository : EntityRepository&lt;Customer, CustomerDescriptor&gt;
    /// {
    ///     protected override int BatchSize { get; init; } = 100; // Custom batch size
    /// }
    /// </code>
    /// </remarks>
    protected virtual int BatchSize { get; init; } = 500;

    /// <summary>
    /// Extracts a batch from the collection, optimizing for common collection types.
    /// </summary>
    /// <param name="entities">The source collection</param>
    /// <param name="startIndex">Starting index for the batch</param>
    /// <param name="batchSize">Size of the batch to extract</param>
    /// <returns>A batch of entities from the collection</returns>
    private static IReadOnlyList<TEntity> GetBatch(IReadOnlyList<TEntity> entities, int startIndex, int batchSize)
    {
        int remainingCount = entities.Count - startIndex;
        int currentBatchSize = Math.Min(batchSize, remainingCount);

        // Optimize for common collection types to avoid multiple enumerations
        if(entities is List<TEntity> list)
        {
            return list.GetRange(startIndex, currentBatchSize);
        }
        else if(entities is TEntity[] array)
        {
            TEntity[] batchArray = new TEntity[currentBatchSize];
            Array.Copy(array, startIndex, batchArray, 0, currentBatchSize);
            return batchArray;
        }
        else
        {
            return entities.Skip(startIndex).Take(currentBatchSize).ToList();
        }
    }

    public async Task<IReadOnlyList<TEntity>> AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        // Batch large operations to prevent parameter overflow and memory issues
        int batchSize = BatchSize;

        if(entities.Count <= batchSize)
        {
            await Set.AddRangeAsync(entities, cancellationToken);
            await SaveChangesWithHandlingAsync(cancellationToken);

            foreach(TEntity entity in entities)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
            }

            return entities;
        }

        // Process in batches - optimize for list/array types
        List<TEntity> results = new(entities.Count);
        for(int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);

            await Set.AddRangeAsync(batch, cancellationToken);
            await SaveChangesWithHandlingAsync(cancellationToken);

            foreach(TEntity entity in batch)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
                results.Add(entity);
            }
        }

        return results;
    }

    public async Task<TEntity?> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        // Optimistic approach: try update first (most common case in production)
        // This avoids race condition of check-then-act pattern
        try
        {
            _ = Set.Update(entity);
            await SaveChangesWithHandlingAsync(cancellationToken);
            DbContext.Entry(entity).State = EntityState.Detached;
            return entity;
        }
        catch(DbUpdateConcurrencyException)
        {
            // Entity doesn't exist, detach and try insert
            DbContext.Entry(entity).State = EntityState.Detached;

            try
            {
                _ = await Set.AddAsync(entity, cancellationToken);
                await SaveChangesWithHandlingAsync(cancellationToken);
                DbContext.Entry(entity).State = EntityState.Detached;
                return entity;
            }
            catch(DbUpdateException ex) when(IsDuplicateKeyException(ex))
            {
                // Race condition: another thread inserted between our attempts
                // Fall back to update - wrap in try-catch to handle potential update failures
                DbContext.Entry(entity).State = EntityState.Detached;
                try
                {
                    _ = Set.Update(entity);
                    await SaveChangesWithHandlingAsync(cancellationToken);
                    DbContext.Entry(entity).State = EntityState.Detached;
                    return entity;
                }
                catch(DbUpdateConcurrencyException)
                {
                    // If final update also fails with concurrency exception, let SaveChangesWithHandlingAsync handle it
                    throw;
                }
                catch(DbUpdateException)
                {
                    // Other database exceptions should propagate for consistent error handling
                    throw;
                }
            }
            catch(DbUpdateException)
            {
                // Non-duplicate-key DbUpdateException: propagate for consistent error handling
                throw;
            }
        }
    }

    /// <summary>
    /// Determines if the given DbUpdateException is caused by a duplicate key violation.
    /// Default implementation checks for common error messages across database providers.
    /// Override in derived classes for database-specific detection using error codes.
    /// </summary>
    /// <remarks>
    /// For production use, consider database-specific implementations:
    /// - SQL Server: Check SqlException with error numbers 2627 (unique constraint) or 2601 (unique index)
    /// - PostgreSQL: Check PostgresException with SqlState "23505"
    /// - MySQL: Check MySqlException with error number 1062
    /// </remarks>
    /// <summary>
    /// Determines if the given DbUpdateException is caused by a duplicate key violation.
    /// Must be implemented in derived classes for database-specific detection.
    /// </summary>
    protected abstract bool IsDuplicateKeyException(DbUpdateException exception);

    public async Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        _ = Set.Update(entity);
        await SaveChangesWithHandlingAsync(cancellationToken);
        DbContext.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public async Task RemoveAsync(TEntity entity, CancellationToken cancellationToken)
    {
        _ = Set.Remove(entity);
        await SaveChangesWithHandlingAsync(cancellationToken);
    }

    public async Task RemoveAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<TEntity> list = await GetAsync(descriptor, cancellationToken);
        await RemoveRangeAsync(list, cancellationToken);
    }

    public async Task UpdateRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        // Batch large operations to prevent parameter overflow and memory issues
        int batchSize = BatchSize;

        if(entities.Count <= batchSize)
        {
            Set.UpdateRange(entities);
            await SaveChangesWithHandlingAsync(cancellationToken);

            foreach(TEntity entity in entities)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
            }
            return;
        }

        // Process in batches - optimize for list/array types
        for(int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);

            Set.UpdateRange(batch);
            await SaveChangesWithHandlingAsync(cancellationToken);

            foreach(TEntity entity in batch)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
            }
        }
    }

    public async Task RemoveRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        // Batch large operations to prevent parameter overflow and memory issues
        int batchSize = BatchSize;

        if(entities.Count <= batchSize)
        {
            Set.RemoveRange(entities);
            await SaveChangesWithHandlingAsync(cancellationToken);
            return;
        }

        // Process in batches - optimize for list/array types
        for(int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);
            Set.RemoveRange(batch);
            await SaveChangesWithHandlingAsync(cancellationToken);
        }
    }

    protected virtual EntityError TranslateException(DbUpdateException exception)
    {
        return EntityError.Conflict("A persistence conflict occurred while saving the entity state.");
    }

    private async Task SaveChangesWithHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch(DbUpdateConcurrencyException ex)
        {
            throw new EntityFailureException(
                EntityError.Concurrency("The entity was modified by another process."),
                ex);
        }
        catch(DbUpdateException ex)
        {
            throw new EntityFailureException(TranslateException(ex), ex);
        }
    }
}
