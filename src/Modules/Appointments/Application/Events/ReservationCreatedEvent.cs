using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationCreatedEvent(Guid ReservationId, Guid SessionOccurrenceId, string? ReservingPartyId) : INotification;
