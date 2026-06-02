namespace Vitarish.Appointments.Application.Events;

public record ShiftReminderPolicyTriggeredEvent(
    Guid ReservationId,
    Guid ShiftId,
    DateOnly Date,
    string? ReservingPartyId,
    ReservationStatus NewStatus) : INotification;
