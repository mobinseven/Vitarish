namespace Vitarish.Appointments.Application.Events;

public record ShiftClosedEvent(Guid ShiftId) : INotification;
