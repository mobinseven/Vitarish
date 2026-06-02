namespace Vitarish.Appointments.Application.Events;

public record ReservationExpiredEvent(Guid ReservationId, Guid ShiftId, DateOnly Date, string? ReservingPartyId) : INotification;
