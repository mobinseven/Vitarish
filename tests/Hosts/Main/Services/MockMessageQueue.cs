using Vitarish.Accounts.Infrastructure.Services;

namespace Vitarish.Main.Mock.Services;

public sealed class MockMessageQueue : IMessageQueue
{
    public int Count => 0;

    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
    }

    public MessageQueueMessage Dequeue()
    {
        throw new InvalidOperationException("The queue is empty.");
    }
}
