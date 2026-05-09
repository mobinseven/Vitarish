namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class AuditLogger(AppointmentsDbContext dbContext) : IAuditLogger
{
    private const string SessionEntityType = nameof(Session);
    private const string ReservationEntityType = nameof(Reservation);

    public async Task LogAsync(AppointmentAuditEntry entry, CancellationToken cancellationToken)
    {
        AuditEntry auditEntry = new()
        {
            Action = entry.Action,
            ActorId = entry.ActorId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            SnapshotBefore = entry.SnapshotBefore,
            SnapshotAfter = entry.SnapshotAfter,
            OccurredAt = entry.OccurredAt
        };

        _ = await dbContext.Set<AuditEntry>().AddAsync(auditEntry, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryReadDto>> GetSessionAuditAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        List<AuditEntry> entries = await dbContext.Set<AuditEntry>()
            .AsNoTracking()
            .Where(e => e.EntityType == SessionEntityType && e.EntityId == sessionId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
        return [.. entries.Select(ToDto)];
    }

    public async Task<IReadOnlyList<AuditEntryReadDto>> GetReservationAuditAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        List<AuditEntry> entries = await dbContext.Set<AuditEntry>()
            .AsNoTracking()
            .Where(e => e.EntityType == ReservationEntityType && e.EntityId == reservationId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
        return [.. entries.Select(ToDto)];
    }

    private static AuditEntryReadDto ToDto(AuditEntry e) => new()
    {
        Id = e.Id,
        Action = e.Action,
        ActorId = e.ActorId,
        EntityType = e.EntityType,
        EntityId = e.EntityId,
        SnapshotBefore = e.SnapshotBefore,
        SnapshotAfter = e.SnapshotAfter,
        OccurredAt = e.OccurredAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        CreatedBy = e.CreatedBy
    };
}
