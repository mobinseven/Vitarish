namespace Vitarish.Appointments.Application.Events;

public record ShiftOpenedEvent(Guid ShiftId) : INotification;
