namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Domain event output bus. Wraps MediatR Publish internally and exposes typed publish methods.
/// </summary>
public interface ICalendarNotificationPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : INotification;
}
