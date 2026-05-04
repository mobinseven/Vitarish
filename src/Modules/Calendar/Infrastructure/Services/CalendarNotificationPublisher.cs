namespace Vitarish.Calendar.Infrastructure.Services;

/// <summary>
/// Thin wrapper over IMediator.Publish that provides the domain event output bus.
/// </summary>
[RegisterTransient]
internal class CalendarNotificationPublisher(IMediator mediator) : ICalendarNotificationPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : INotification =>
        mediator.Publish(@event, ct);
}
