using Hangfire;
using Vitarish.Interfaces.Accounts.Services;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Communications;

/// <summary>
/// Hangfire-backed implementation of IMessageQueue that persists messages to storage.
/// </summary>
public class HangfireMessageQueue(IBackgroundJobClient backgroundJobClient) : IMessageQueue
{
    public int Count => 0;

    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to, nameof(to));
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));

        EmailMessage message = new(to, subject, textBody, htmlBody);
        _ = backgroundJobClient.Enqueue<MessageProcessor>(processor => processor.ProcessMessage(message));
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber, nameof(phoneNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        SmsMessage message = new(phoneNumber, code);
        _ = backgroundJobClient.Enqueue<MessageProcessor>(processor => processor.ProcessMessage(message));
    }

    public MessageQueueMessage Dequeue() => throw new NotSupportedException("Hangfire-backed message queue does not support dequeue operations.");
}
