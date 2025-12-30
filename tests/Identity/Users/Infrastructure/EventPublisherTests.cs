using MediatR;
using Vitarish.Users.Infrastructure.Service;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_LogsAndPublishesEvent()
    {
        TestLogger<EventPublisher> logger = new();
        Mock<IPublisher> mediator = new();
        EventPublisher publisher = new(logger, mediator.Object);
        TestNotification notification = new("payload");

        await publisher.PublishAsync(notification, CancellationToken.None);

        mediator.Verify(m => m.Publish(notification, It.IsAny<CancellationToken>()), Times.Once);
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("Publishing"));
    }

    private sealed record TestNotification(string Payload) : INotification;
}
