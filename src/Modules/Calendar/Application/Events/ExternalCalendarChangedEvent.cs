namespace Vitarish.Calendar.Application.Events;

public record ExternalCalendarChangedEvent(Guid AccountId, IReadOnlyList<string> ChangedResourceUrls) : INotification;
