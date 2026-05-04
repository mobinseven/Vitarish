namespace Vitarish.Calendar.Application.Events;

public record AppointmentMovedEvent(Guid MappingId, DateTimeOffset PreviousStart, DateTimeOffset NewStart) : INotification;
