using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationMovedEvent(Guid ReservationId, Guid FromOccurrenceId, Guid ToOccurrenceId, string? ReservingPartyId) : INotification;
