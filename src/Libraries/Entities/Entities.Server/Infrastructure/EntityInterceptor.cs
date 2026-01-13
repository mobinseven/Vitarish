using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Entities.Server.Services;

namespace Vitarish.Entities.Server.Infrastructure;

public sealed class EntityInterceptor(
        IEntityTimeService timeService,
        IEntityUserService userService,
        IEntityCacheInvalidationService cacheInvalidationService,
        IPublisher publisher) : SaveChangesInterceptor
{

    private readonly ConcurrentDictionary<DbContext, List<EntityNotificationCore>> _notifications = new(10, 100); // TODO: adjust these numbers with realistic scenarios. This interceptor is used per scope so it won't grow too much.


    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is DbContext context)
        {
            DateTimeOffset now = timeService.Now;
            UpdateEntities(context, now, userService);
            CaptureChanges(context, now);
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
            UpdateEntities(context, now, userService);
            CaptureChanges(context, now);
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish notifications: {ex.Message}");
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
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
        {
            _ = _notifications.TryRemove(eventData.Context, out _);
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
        }

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static void UpdateEntities(DbContext context, DateTimeOffset now, IEntityUserService userService)
    {
        context.ChangeTracker.CascadeChanges();

        string? userId = userService.UserId;

        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
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

    private void CaptureChanges(DbContext context, DateTimeOffset now)
    {
        List<EntityNotificationCore> list = [];

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

            Dictionary<string, object?> snapshot = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

            IReadOnlyDictionary<string, (object? Old, object? New)>? changes = null;
            if (entry.State == EntityState.Modified)
            {
                Dictionary<string, (object? Old, object? New)> c = [];
                foreach (PropertyEntry prop in entry.Properties)
                {
                    if (prop.IsModified)
                    {
                        c[prop.Metadata.Name] = (prop.OriginalValue, prop.CurrentValue);
                    }
                }

                if (c.Count > 0)
                {
                    changes = c;
                }
            }

            EntityNotificationArgs args = new(
                entry.Entity.GetType().Name,
                entry.Entity.Id.ToString(),
                operation.Value,
                now,
                Snapshot: snapshot,
                Changes: changes);

            list.Add(new EntityNotificationCore(args));
        }

        if (list.Count > 0)
        {
            _notifications[context] = list;
        }
    }

    private async Task PublishNotificationsAsync(DbContext? context, IPublisher publisher, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        if (_notifications.TryRemove(context, out List<EntityNotificationCore>? list))
        {
            foreach (EntityNotificationCore notification in list)
            {
                await publisher.Publish(notification, cancellationToken);
            }
        }
    }
}
