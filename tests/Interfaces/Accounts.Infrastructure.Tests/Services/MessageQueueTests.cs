using Vitarish.Accounts.Infrastructure.Services;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class MessageQueueTests
{
    [Fact]
    public void MessageQueue_Enqueue_AddsMessage()
    {
        MessageQueue queue = new();

        queue.EnqueueEmail("user@example.com", "Subject", "Text", "Html");

        queue.Count.Should().Be(1);
        queue.Dequeue().Should().BeOfType<EmailQueueMessage>();
    }

    [Fact]
    public void MessageQueue_Dequeue_ReturnsInOrder()
    {
        MessageQueue queue = new();
        queue.EnqueueEmail("user@example.com", "Subject", "Text", "Html");
        queue.EnqueueSms("+123", "1234");

        MessageQueueMessage first = queue.Dequeue();
        MessageQueueMessage second = queue.Dequeue();

        first.Should().BeOfType<EmailQueueMessage>();
        second.Should().BeOfType<SmsQueueMessage>();
    }
}
