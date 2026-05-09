using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Entities.Server.Infrastructure;

public sealed class EntityInterceptor(
        IEntityTimeService timeService,
        IEntityUserService userService,
        IEntityCacheInvalidationService cacheInvalidationService,
        IPublisher publisher) : SaveChangesInterceptor
{
    private const int MaxPayloadLength = 16000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly HashSet<string> IgnoredAuditProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Entity.CreatedAt),
        nameof(Entity.CreatedBy),
        nameof(Entity.UpdatedAt),
        nameof(Entity.UpdatedBy),
        nameof(Entity.DeletedAt),
        nameof(Entity.DeletedBy)
    };
    private static readonly string[] SensitiveMarkers =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "authorization",
        "credential",
        "cookie",
        "bearer",
        "hash",
        "salt"
    ];

    private readonly ConcurrentDictionary<DbContext, List<EntityNotification>> _notifications = new(10, 100); // TODO: adjust these numbers with realistic scenarios. This interceptor is used per scope so it won't grow too much.


    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is DbContext context)
        {
            DateTimeOffset now = timeService.Now;
            UpdateEntities(context, now, userService);
            AppendAuditEntries(context, now, userService);
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
            AppendAuditEntries(context, now, userService);
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
                    // Skip soft delete conversion if entity is marked for hard delete
                    if (!entry.Entity.IsMarkedForHardDelete)
                    {
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.DeletedBy = userId;
                    }
                    break;
            }
        }
    }

    private static void AppendAuditEntries(DbContext context, DateTimeOffset now, IEntityUserService userService)
    {
        if (context.Model.FindEntityType(typeof(AuditEntry)) is null)
        {
            return;
        }

        List<EntityEntry<Entity>> trackedEntries = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity is not AuditEntry && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (trackedEntries.Count == 0)
        {
            return;
        }

        string? actorId = userService.UserId;
        List<AuditEntry> auditEntries = [];

        foreach (EntityEntry<Entity> trackedEntry in trackedEntries)
        {
            (string? action, string? before, string? after, string? changes) = CaptureAuditPayload(trackedEntry);
            if (action is null)
            {
                continue;
            }

            string? tenantId = ResolveTenantId(trackedEntry);
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                continue;
            }

            auditEntries.Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                Action = action,
                ActorId = actorId,
                EntityType = trackedEntry.Entity.GetType().Name,
                EntityId = trackedEntry.Entity.Id,
                SnapshotBefore = before,
                SnapshotAfter = after,
                Changes = changes,
                OccurredAt = now,
                CorrelationId = null,
                RequestId = null
            });
        }

        if (auditEntries.Count == 0)
        {
            return;
        }

        context.Set<AuditEntry>().AddRange(auditEntries);
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
        List<EntityNotification> list = [];

        foreach (EntityEntry<Entity> entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.Entity is AuditEntry)
            {
                continue;
            }

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

            EntityNotificationArgs args = new()
            {
                EntityName = entry.Entity.GetType().Name,
                Id = entry.Entity.Id.ToString(),
                Operation = operation.Value,
                OccurredAt = now,
                CorrelationId = entry.Property("TenantId").CurrentValue?.ToString()
            };

            list.Add(new EntityNotification(args));
        }

        if (list.Count > 0)
        {
            _notifications[context] = list;
        }
    }

    private static (string? Action, string? Before, string? After, string? Changes) CaptureAuditPayload(EntityEntry<Entity> entry)
    {
        string? action = entry.State switch
        {
            EntityState.Added => "Created",
            EntityState.Modified when entry.Property(nameof(Entity.DeletedAt)).IsModified && entry.Entity.DeletedAt != null => "Deleted",
            EntityState.Modified => "Updated",
            EntityState.Deleted => "Deleted",
            _ => null
        };

        if (action is null)
        {
            return (null, null, null, null);
        }

        Dictionary<string, object?>? before = entry.State is EntityState.Modified or EntityState.Deleted
            ? GetSnapshot(entry, currentValues: false)
            : null;
        Dictionary<string, object?>? after = entry.State is EntityState.Added or EntityState.Modified
            ? GetSnapshot(entry, currentValues: true)
            : null;
        Dictionary<string, (object? Old, object? New)>? changes = entry.State == EntityState.Modified
            ? GetChanges(entry)
            : null;

        return (
            action,
            SerializeWithLimit(before),
            SerializeWithLimit(after),
            SerializeWithLimit(changes));
    }

    private static Dictionary<string, object?> GetSnapshot(EntityEntry<Entity> entry, bool currentValues)
    {
        Dictionary<string, object?> snapshot = [];
        PropertyValues values = currentValues ? entry.CurrentValues : entry.OriginalValues;

        foreach (PropertyEntry propertyEntry in entry.Properties)
        {
            IProperty metadata = propertyEntry.Metadata;
            if (!ShouldCaptureProperty(metadata))
            {
                continue;
            }

            object? value = values[metadata];
            snapshot[metadata.Name] = SanitizeValue(metadata.Name, value);
        }

        return snapshot;
    }

    private static Dictionary<string, (object? Old, object? New)> GetChanges(EntityEntry<Entity> entry)
    {
        Dictionary<string, (object? Old, object? New)> changes = [];

        foreach (PropertyEntry propertyEntry in entry.Properties.Where(p => p.IsModified))
        {
            IProperty metadata = propertyEntry.Metadata;
            if (!ShouldCaptureProperty(metadata))
            {
                continue;
            }

            object? original = SanitizeValue(metadata.Name, propertyEntry.OriginalValue);
            object? current = SanitizeValue(metadata.Name, propertyEntry.CurrentValue);
            changes[metadata.Name] = (original, current);
        }

        return changes;
    }

    private static bool ShouldCaptureProperty(IProperty property) =>
        !property.IsIndexerProperty() &&
        !property.IsShadowProperty() &&
        !IgnoredAuditProperties.Contains(property.Name);

    private static object? SanitizeValue(string name, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (SensitiveMarkers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return "[REDACTED]";
        }

        return value switch
        {
            byte[] bytes => $"[binary:{bytes.Length}]",
            string text when text.Length > 2048 => $"{text[..2048]}…[truncated]",
            _ => value
        };
    }

    private static string? SerializeWithLimit<T>(T? value)
    {
        if (value is null)
        {
            return null;
        }

        string payload = JsonSerializer.Serialize(value, JsonOptions);
        return payload.Length <= MaxPayloadLength
            ? payload
            : $"{payload[..MaxPayloadLength]}…[truncated]";
    }

    private static string? ResolveTenantId(EntityEntry<Entity> entry)
    {
        PropertyEntry? tenantProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
        string? tenantId = tenantProperty?.CurrentValue?.ToString();
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId;
        }

        return tenantProperty?.OriginalValue?.ToString();
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
}
