namespace Vitarish.Calendar.Application.Events;

public record OccurrenceCancelledEvent(Guid MappingId, string RecurrenceId, OccurrenceEditScope Scope) : INotification;
