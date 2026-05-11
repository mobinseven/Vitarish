using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationExpiredEvent(Guid ReservationId, Guid SessionId, DateOnly Date, string? ReservingPartyId) : INotification;
