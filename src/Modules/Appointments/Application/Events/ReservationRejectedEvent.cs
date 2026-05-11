using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationRejectedEvent(Guid ReservationId, Guid SessionId, DateOnly Date, string? ReservingPartyId) : INotification;
