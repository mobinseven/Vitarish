using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Entities.Server.Infrastructure;

public sealed class EntityInterceptor(
        IEntityTimeService timeService,
        IEntityUserService userService,
        IEntityCacheInvalidationService cacheInvalidationService,
        IPublisher publisher,
        IAuditWriter? auditWriter = null) : SaveChangesInterceptor
{

    private readonly ConcurrentDictionary<DbContext, List<EntityNotification>> _notifications = new(10, 100);
    private readonly ConcurrentDictionary<DbContext, List<AuditEntry>> _auditEntries = new(10, 100);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is DbContext context)
        {
            DateTimeOffset now = timeService.Now;
            UpdateEntities(context, now, userService.UserId);
            CaptureChanges(context, now, userService.UserId);
            InvalidateCache(context, cacheInvalidationService);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbContext context)
        {
            DateTimeOffset now = timeService.Now;
            UpdateEntities(context, now, userService.UserId);
            CaptureChanges(context, now, userService.UserId);
            InvalidateCache(context, cacheInvalidationService);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await PublishNotificationsAsync(eventData.Context, publisher, CancellationToken.None);
                await PersistAuditEntriesAsync(eventData.Context, auditWriter, CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to process post-save events: {ex.Message}");
            }
        });

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await PublishNotificationsAsync(eventData.Context, publisher, cancellationToken);
        await PersistAuditEntriesAsync(eventData.Context, auditWriter, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
        {
            _ = _notifications.TryRemove(eventData.Context, out _);
            _ = _auditEntries.TryRemove(eventData.Context, out _);
        }

        base.SaveChangesFailed(eventData);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            _ = _notifications.TryRemove(eventData.Context, out _);
            _ = _auditEntries.TryRemove(eventData.Context, out _);
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static void UpdateEntities(DbContext context, DateTimeOffset now, string? actorId)
    {
        context.ChangeTracker.CascadeChanges();

        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actorId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actorId;
                    break;
                case EntityState.Deleted:
                    if (!entry.Entity.IsMarkedForHardDelete)
                    {
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.DeletedBy = actorId;
                    }
                    break;
            }
        }
    }

    private static void InvalidateCache(DbContext context, IEntityCacheInvalidationService cacheInvalidatorService)
    {
        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            cacheInvalidatorService.Invalidate(entry.Entity, entry.State);
        }
    }

    private void CaptureChanges(DbContext context, DateTimeOffset now, string? actorId)
    {
        List<EntityNotification> notifications = [];
        List<AuditEntry> auditEntries = [];

        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            EntityOperation? operation = entry.State switch
            {
                EntityState.Added => EntityOperation.Created,
                EntityState.Modified when entry.Property(nameof(Entity.DeletedAt)).IsModified && entry.Entity.DeletedAt != null => EntityOperation.Deleted,
                EntityState.Modified => EntityOperation.Updated,
                EntityState.Deleted => EntityOperation.Deleted,
                _ => null
            };

            if (operation is null)
            {
                continue;
            }

            string? tenantId = TryGetTenantId(entry);

            IReadOnlyDictionary<string, object?>? snapshotBefore = GetSnapshot(entry.OriginalValues, includePrimaryKey: true);
            IReadOnlyDictionary<string, object?>? snapshotAfter = GetSnapshot(entry.CurrentValues, includePrimaryKey: true);
            IReadOnlyDictionary<string, (object? Old, object? New)>? changes = GetChanges(entry);

            EntityNotificationArgs args = new()
            {
                EntityName = entry.Entity.GetType().Name,
                Id = entry.Entity.Id.ToString(),
                Operation = operation.Value,
                OccurredAt = now,
                CorrelationId = tenantId,
                Snapshot = operation.Value switch
                {
                    EntityOperation.Created => snapshotAfter,
                    EntityOperation.Deleted => snapshotBefore,
                    _ => snapshotAfter
                },
                Changes = changes
            };

            notifications.Add(new EntityNotification(args));

            if (tenantId is null)
            {
                continue;
            }

            AuditEntry auditEntry = new()
            {
                Id = Guid.NewGuid(),
                Action = MapAction(operation.Value),
                ActorId = actorId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id,
                SnapshotBefore = ShouldIncludeBefore(operation.Value) ? JsonSerializer.Serialize(snapshotBefore) : null,
                SnapshotAfter = ShouldIncludeAfter(operation.Value) ? JsonSerializer.Serialize(snapshotAfter) : null,
                OccurredAt = now,
                TenantId = tenantId
            };

            auditEntries.Add(auditEntry);
        }

        if (notifications.Count > 0)
        {
            _notifications[context] = notifications;
        }

        if (auditEntries.Count > 0)
        {
            _auditEntries[context] = auditEntries;
        }
    }

    private async Task PublishNotificationsAsync(DbContext? context, IPublisher publisher, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        if (_notifications.TryRemove(context, out List<EntityNotification>? list))
        {
            foreach (EntityNotification notification in list)
            {
                await publisher.Publish(notification, cancellationToken);
            }
        }
    }

    private async Task PersistAuditEntriesAsync(DbContext? context, IAuditWriter? writer, CancellationToken cancellationToken)
    {
        if (context is null || writer is null)
        {
            return;
        }

        if (_auditEntries.TryRemove(context, out List<AuditEntry>? entries) && entries.Count > 0)
        {
            await writer.WriteBatchAsync(entries, cancellationToken);
        }
    }

    private static string? TryGetTenantId(EntityEntry<Entity> entry)
    {
        PropertyEntry? tenantProperty = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "TenantId", StringComparison.Ordinal));
        return tenantProperty?.CurrentValue?.ToString();
    }

    private static bool ShouldIncludeBefore(EntityOperation operation) => operation is EntityOperation.Updated or EntityOperation.Deleted;

    private static bool ShouldIncludeAfter(EntityOperation operation) => operation is EntityOperation.Created or EntityOperation.Updated;

    private static string MapAction(EntityOperation operation) => operation switch
    {
        EntityOperation.Created => AuditAction.Created,
        EntityOperation.Updated => AuditAction.Updated,
        EntityOperation.Deleted => AuditAction.Deleted,
        _ => operation.ToString()
    };

    private static IReadOnlyDictionary<string, object?> GetSnapshot(PropertyValues values, bool includePrimaryKey)
    {
        Dictionary<string, object?> snapshot = new(StringComparer.Ordinal);
        foreach (IProperty propertyMetadata in values.Properties)
        {
            if (!includePrimaryKey && propertyMetadata.IsPrimaryKey())
            {
                continue;
            }

            snapshot[propertyMetadata.Name] = values[propertyMetadata.Name];
        }

        return snapshot;
    }

    private static IReadOnlyDictionary<string, (object? Old, object? New)>? GetChanges(EntityEntry<Entity> entry)
    {
        Dictionary<string, (object? Old, object? New)> changes = new(StringComparer.Ordinal);

        switch (entry.State)
        {
            case EntityState.Added:
                foreach (PropertyEntry property in entry.Properties)
                {
                    changes[property.Metadata.Name] = (null, property.CurrentValue);
                }
                break;

            case EntityState.Deleted:
                foreach (PropertyEntry property in entry.Properties)
                {
                    changes[property.Metadata.Name] = (property.OriginalValue, null);
                }
                break;

            case EntityState.Modified:
                foreach (PropertyEntry property in entry.Properties.Where(p => p.IsModified))
                {
                    changes[property.Metadata.Name] = (property.OriginalValue, property.CurrentValue);
                }
                break;
        }

        return changes.Count == 0 ? null : changes;
    }
}
