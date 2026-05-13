using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationRejectedEvent(Guid ReservationId, Guid ShiftId, DateOnly Date, string? ReservingPartyId) : INotification;
