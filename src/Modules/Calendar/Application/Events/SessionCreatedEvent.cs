namespace Vitarish.Calendar.Application.Events;

public record SessionCreatedEvent(Guid MappingId, Guid AccountId, DateTimeOffset OccurrenceStart) : INotification;
