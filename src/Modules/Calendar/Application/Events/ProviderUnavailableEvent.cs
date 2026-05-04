namespace Vitarish.Calendar.Application.Events;

public record ProviderUnavailableEvent(Guid AccountId, TimeRangeQuery BlockedRange) : INotification;
