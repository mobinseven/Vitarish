namespace Vitarish.Calendar.Application.Events;

public record SyncConflictDetectedEvent(Guid AccountId, Guid MappingId, string ConflictDescription) : INotification;
