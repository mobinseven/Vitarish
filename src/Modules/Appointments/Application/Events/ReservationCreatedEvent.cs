namespace Vitarish.Appointments.Application.Events;

public record ReservationCreatedEvent(Guid ReservationId, Guid ShiftId, DateOnly Date, string? ReservingPartyId) : INotification;
