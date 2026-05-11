using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationCancelledEvent(Guid ReservationId, Guid SessionId, DateOnly Date, string? ReservingPartyId) : INotification;
