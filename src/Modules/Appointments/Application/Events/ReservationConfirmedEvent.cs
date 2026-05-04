using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationConfirmedEvent(Guid ReservationId, Guid SessionOccurrenceId, string? ReservingPartyId) : INotification;
