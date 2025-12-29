namespace Vitarish.Parties.Tests.TestHelpers;

using MediatR;

/// <summary>
/// Publisher that records notifications for assertions.
/// </summary>
internal sealed class RecordingPublisher : IPublisher
{
    private readonly List<object> _notifications = [];

    public IReadOnlyCollection<object> Notifications => _notifications.AsReadOnly();

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        _notifications.Add(notification!);
        return Task.CompletedTask;
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        _notifications.Add(notification);
        return Task.CompletedTask;
    }
}
