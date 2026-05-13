using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationCancelledEvent(Guid ReservationId, Guid ShiftId, DateOnly Date, string? ReservingPartyId) : INotification;
