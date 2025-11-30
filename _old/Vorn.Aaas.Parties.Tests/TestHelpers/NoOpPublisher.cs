namespace Vorn.Aaas.Parties.Tests.TestHelpers;

using MediatR;

/// <summary>
/// No-op publisher for testing purposes.
/// </summary>
internal sealed class NoOpPublisher : IPublisher
{
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
