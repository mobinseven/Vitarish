using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class AuditQueryService(AuditDbContext dbContext) : IAuditQueryService
{
    public async Task<IReadOnlyList<AuditEntryReadDto>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken) => await dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new AuditEntryReadDto
            {
                Id = e.Id,
                Action = e.Action,
                ActorId = e.ActorId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                SnapshotBefore = e.SnapshotBefore,
                SnapshotAfter = e.SnapshotAfter,
                OccurredAt = e.OccurredAt,
                TenantId = e.TenantId
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditEntryReadDto>> GetByActorAsync(string actorId, CancellationToken cancellationToken) => await dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.ActorId == actorId)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new AuditEntryReadDto
            {
                Id = e.Id,
                Action = e.Action,
                ActorId = e.ActorId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                SnapshotBefore = e.SnapshotBefore,
                SnapshotAfter = e.SnapshotAfter,
                OccurredAt = e.OccurredAt,
                TenantId = e.TenantId
            })
            .ToListAsync(cancellationToken);
}
