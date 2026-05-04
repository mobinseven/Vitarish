using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationRejectedEvent(Guid ReservationId, Guid SessionOccurrenceId, string? ReservingPartyId) : INotification;
