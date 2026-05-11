using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationCreatedEvent(Guid ReservationId, Guid SessionId, DateOnly Date, string? ReservingPartyId) : INotification;
