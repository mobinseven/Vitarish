using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(AppointmentAuditEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntryReadDto>> GetSessionAuditAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntryReadDto>> GetReservationAuditAsync(Guid reservationId, CancellationToken cancellationToken);
}
