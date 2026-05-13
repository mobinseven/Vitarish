using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationConfirmedEvent(Guid ReservationId, Guid ShiftId, DateOnly Date, string? ReservingPartyId) : INotification;
