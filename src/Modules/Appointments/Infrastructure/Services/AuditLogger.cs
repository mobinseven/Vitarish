namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class AuditLogger(AuditRepository auditRepository) : IAuditLogger
{
    private const string SessionEntityType = nameof(Session);
    private const string ReservationEntityType = nameof(Reservation);

    public async Task LogAsync(AppointmentAuditEntry entry, CancellationToken cancellationToken)
    {
        _ = await auditRepository.AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryReadDto>> GetSessionAuditAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppointmentAuditEntry> entries = await auditRepository.GetByEntityAsync(SessionEntityType, sessionId, cancellationToken);
        return [.. entries.Select(ToDto)];
    }

    public async Task<IReadOnlyList<AuditEntryReadDto>> GetReservationAuditAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppointmentAuditEntry> entries = await auditRepository.GetByEntityAsync(ReservationEntityType, reservationId, cancellationToken);
        return [.. entries.Select(ToDto)];
    }

    private static AuditEntryReadDto ToDto(AppointmentAuditEntry e) => new()
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
