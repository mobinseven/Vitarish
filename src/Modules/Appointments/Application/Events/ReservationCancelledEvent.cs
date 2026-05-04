using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationCancelledEvent(Guid ReservationId, Guid SessionOccurrenceId, string? ReservingPartyId) : INotification;
