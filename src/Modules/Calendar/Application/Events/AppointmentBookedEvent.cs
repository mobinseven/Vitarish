namespace Vitarish.Calendar.Application.Events;

public record AppointmentBookedEvent(Guid MappingId, Guid AccountId, string UserId, DateTimeOffset OccurrenceStart) : INotification;
