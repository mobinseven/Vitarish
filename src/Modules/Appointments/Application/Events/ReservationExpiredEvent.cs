using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationExpiredEvent(Guid ReservationId, Guid SessionOccurrenceId, string? ReservingPartyId) : INotification;
