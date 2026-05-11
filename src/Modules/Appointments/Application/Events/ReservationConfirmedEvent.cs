using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationConfirmedEvent(Guid ReservationId, Guid SessionId, DateOnly Date, string? ReservingPartyId) : INotification;
