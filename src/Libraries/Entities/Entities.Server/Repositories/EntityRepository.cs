using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Entities.Server.Repositories.Operations;

namespace Vitarish.Entities.Server.Repositories;

public abstract class EntityRepository<TEntity, TDescriptor, TDbContext>(TDbContext dbContext)
    : IEntityRepository<TEntity, TDescriptor>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; } = dbContext;

    /// <summary>
    /// Exposes the default tracked set on the constructor-injected context.
    /// Available for derived classes that have not yet migrated to operation-scoped contexts.
    /// </summary>
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    /// <summary>
    /// Creates a context scope for a single repository operation.
    /// The default implementation returns an ambient scope that wraps the constructor-injected
    /// context, preserving existing behavior with zero overhead.
    /// Override within this assembly to return a <see cref="TransientEntityContextScope{TDbContext}"/>
    /// for full per-operation context isolation.
    /// </summary>
    private protected virtual IEntityContextScope<TDbContext> CreateOperationScope()
        => new AmbientEntityContextScope<TDbContext>(DbContext);

    protected virtual IQueryable<TEntity> ApplyCore(IQueryable<TEntity> query, TDescriptor descriptor)
    {
        query = Apply(query, descriptor);

        if (query is not IOrderedQueryable<TEntity>)
        {
            query = query.OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt);
        }

        return query.ApplyPagination(descriptor);
    }

    protected virtual IQueryable<TEntity> Apply(IQueryable<TEntity> query, TDescriptor descriptor) => query.ApplyDescription(descriptor);

    public async Task<int> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        return await ApplyCore(scope.Context.Set<TEntity>().AsNoTracking(), descriptor).CountAsync(cancellationToken);
    }

    public async Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        return await scope.Context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        return await ApplyCore(scope.Context.Set<TEntity>().AsNoTracking(), descriptor).ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        EntityEntry<TEntity> entry = await scope.Context.Set<TEntity>().AddAsync(entity, cancellationToken);
        await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
        scope.Context.Entry(entity).State = EntityState.Detached;
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
        if (entities is List<TEntity> list)
        {
            return list.GetRange(startIndex, currentBatchSize);
        }
        else if (entities is TEntity[] array)
        {
            TEntity[] batchArray = new TEntity[currentBatchSize];
            Array.Copy(array, startIndex, batchArray, 0, currentBatchSize);
            return batchArray;
        }
        else
        {
            return [.. entities.Skip(startIndex).Take(currentBatchSize)];
        }
    }

    public async Task<IReadOnlyList<TEntity>> AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        DbSet<TEntity> set = scope.Context.Set<TEntity>();
        int batchSize = BatchSize;

        if (entities.Count <= batchSize)
        {
            await set.AddRangeAsync(entities, cancellationToken);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);

            foreach (TEntity entity in entities)
            {
                scope.Context.Entry(entity).State = EntityState.Detached;
            }

            return entities;
        }

        // Batch large operations to prevent parameter overflow and memory issues
        List<TEntity> results = new(entities.Count);
        for (int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);

            await set.AddRangeAsync(batch, cancellationToken);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);

            foreach (TEntity entity in batch)
            {
                scope.Context.Entry(entity).State = EntityState.Detached;
                results.Add(entity);
            }
        }

        return results;
    }

    public async Task<TEntity?> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const int maxAttempts = 3;

        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();

        TDbContext context = scope.Context;
        DbSet<TEntity> set = context.Set<TEntity>();

        IEntityType entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' is not part of the current DbContext model.");

        IKey primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have a primary key.");

        if (primaryKey.Properties.Any(p => p.IsShadowProperty()))
        {
            throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' has a shadow primary key. Generic AddOrUpdate cannot resolve it safely.");
        }

        object?[] keyValues = GetPrimaryKeyValues(context, entity, primaryKey);

        bool hasUsableKey = HasUsablePrimaryKey(keyValues);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!hasUsableKey)
                {
                    await set.AddAsync(entity, cancellationToken);
                    await SaveChangesWithHandlingAsync(context, cancellationToken);

                    DetachIfTracked(context, entity);
                    return entity;
                }

                TEntity? existing = await set.FindAsync(keyValues, cancellationToken);

                if (existing is null)
                {
                    await set.AddAsync(entity, cancellationToken);
                    await SaveChangesWithHandlingAsync(context, cancellationToken);

                    DetachIfTracked(context, entity);
                    return entity;
                }

                ApplyValues(context, entityType, source: entity, target: existing);

                await SaveChangesWithHandlingAsync(context, cancellationToken);

                DetachIfTracked(context, existing);

                if (!ReferenceEquals(existing, entity))
                {
                    DetachIfTracked(context, entity);
                }

                return existing;
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex) && attempt < maxAttempts)
            {
                // Another request inserted the row between FindAsync and AddAsync.
                // Clear tracking and retry; next pass should FindAsync the row and update it.
                context.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // Row was changed/deleted between read and save.
                // Clear and retry against the latest database state.
                context.ChangeTracker.Clear();
            }
            catch
            {
                context.ChangeTracker.Clear();
                throw;
            }
        }

        throw new DbUpdateConcurrencyException(
            $"Could not add or update '{typeof(TEntity).Name}' after {maxAttempts} attempts because of repeated concurrency conflicts.");
    }

    private static object?[] GetPrimaryKeyValues(
        TDbContext context,
        TEntity entity,
        IKey primaryKey)
    {
        EntityEntry<TEntity> entry = context.Entry(entity);

        object?[] values = primaryKey.Properties
            .Select(property => entry.Property(property.Name).CurrentValue)
            .ToArray();

        entry.State = EntityState.Detached;

        return values;
    }

    private static bool HasUsablePrimaryKey(object?[] keyValues)
    {
        return keyValues.All(value => value is not null && !IsDefaultValue(value));
    }

    private static bool IsDefaultValue(object value)
    {
        Type type = value.GetType();

        if (!type.IsValueType)
        {
            return false;
        }

        object? defaultValue = Activator.CreateInstance(type);

        return Equals(value, defaultValue);
    }

    private static void ApplyValues(
        TDbContext context,
        IEntityType entityType,
        TEntity source,
        TEntity target)
    {
        EntityEntry<TEntity> sourceEntry = context.Entry(source);
        EntityEntry<TEntity> targetEntry = context.Entry(target);

        foreach (IProperty property in entityType.GetProperties())
        {
            if (property.IsShadowProperty())
            {
                continue;
            }

            if (property.IsPrimaryKey())
            {
                continue;
            }

            if (property.GetAfterSaveBehavior() == PropertySaveBehavior.Throw)
            {
                continue;
            }

            if (property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
            {
                continue;
            }

            object? value = sourceEntry.Property(property.Name).CurrentValue;

            targetEntry.Property(property.Name).CurrentValue = value;
        }

        if (!ReferenceEquals(source, target))
        {
            sourceEntry.State = EntityState.Detached;
        }
    }

    private static void DetachIfTracked(TDbContext context, TEntity entity)
    {
        EntityEntry<TEntity> entry = context.Entry(entity);

        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Determines if the given DbUpdateException is caused by a duplicate key violation.
    /// Must be implemented in derived classes for database-specific detection.
    /// </summary>
    /// <remarks>
    /// For production use, consider database-specific implementations:
    /// - SQL Server: Check SqlException with error numbers 2627 (unique constraint) or 2601 (unique index)
    /// - PostgreSQL: Check PostgresException with SqlState "23505"
    /// - MySQL: Check MySqlException with error number 1062
    /// </remarks>
    protected abstract bool IsDuplicateKeyException(DbUpdateException exception);

    public async Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        _ = scope.Context.Set<TEntity>().Update(entity);
        await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
        scope.Context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public async Task RemoveAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        _ = scope.Context.Set<TEntity>().Remove(entity);
        await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
    }

    public async Task RemoveAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<TEntity> list = await GetAsync(descriptor, cancellationToken);
        await RemoveRangeAsync(list, cancellationToken);
    }

    public async Task UpdateRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        DbSet<TEntity> set = scope.Context.Set<TEntity>();
        int batchSize = BatchSize;

        if (entities.Count <= batchSize)
        {
            set.UpdateRange(entities);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);

            foreach (TEntity entity in entities)
            {
                scope.Context.Entry(entity).State = EntityState.Detached;
            }
            return;
        }

        // Batch large operations to prevent parameter overflow and memory issues
        for (int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);

            set.UpdateRange(batch);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);

            foreach (TEntity entity in batch)
            {
                scope.Context.Entry(entity).State = EntityState.Detached;
            }
        }
    }

    public async Task RemoveRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        DbSet<TEntity> set = scope.Context.Set<TEntity>();
        int batchSize = BatchSize;

        if (entities.Count <= batchSize)
        {
            set.RemoveRange(entities);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
            return;
        }

        // Batch large operations to prevent parameter overflow and memory issues
        for (int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);
            set.RemoveRange(batch);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
        }
    }

    public async Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        entity.HardDelete();
        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        _ = scope.Context.Set<TEntity>().Remove(entity);
        await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
    }

    public async Task HardDeleteAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<TEntity> list = await GetAsync(descriptor, cancellationToken);
        await HardDeleteRangeAsync(list, cancellationToken);
    }

    public async Task HardDeleteRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        foreach (TEntity entity in entities)
        {
            entity.HardDelete();
        }

        await using IEntityContextScope<TDbContext> scope = CreateOperationScope();
        DbSet<TEntity> set = scope.Context.Set<TEntity>();
        int batchSize = BatchSize;

        if (entities.Count <= batchSize)
        {
            set.RemoveRange(entities);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
            return;
        }

        // Batch large operations to prevent parameter overflow and memory issues
        for (int i = 0; i < entities.Count; i += batchSize)
        {
            IReadOnlyList<TEntity> batch = GetBatch(entities, i, batchSize);
            set.RemoveRange(batch);
            await SaveChangesWithHandlingAsync(scope.Context, cancellationToken);
        }
    }

    protected virtual EntityError TranslateException(DbUpdateException exception) => EntityError.Conflict("A persistence conflict occurred while saving the entity state.");

    private async Task SaveChangesWithHandlingAsync(TDbContext ctx, CancellationToken cancellationToken)
    {
        try
        {
            _ = await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new EntityFailureException(
                EntityError.Concurrency("The entity was modified by another process."),
                ex);
        }
        catch (DbUpdateException ex)
        {
            throw new EntityFailureException(TranslateException(ex), ex);
        }
    }
}
