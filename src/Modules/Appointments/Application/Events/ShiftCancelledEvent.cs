namespace Vitarish.Appointments.Application.Events;

public record ShiftCancelledEvent(Guid ShiftId) : INotification;
