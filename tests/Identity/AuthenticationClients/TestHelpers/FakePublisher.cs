using MediatR;

namespace Vitarish.AuthenticationClients.Tests.TestHelpers;

/// <summary>
/// A no-op MediatR publisher for testing purposes.
/// </summary>
public sealed class FakePublisher : IPublisher
{
    public List<INotification> PublishedNotifications { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is INotification n)
        {
            PublishedNotifications.Add(n);
        }
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        PublishedNotifications.Add(notification);
        return Task.CompletedTask;
    }
}
