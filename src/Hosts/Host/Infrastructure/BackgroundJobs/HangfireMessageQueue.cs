using Hangfire;
using Vitarish.Interface.Infrastructure.Services;

namespace Vitarish.Host.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-backed implementation of IMessageQueue that persists messages to storage.
/// </summary>
public class HangfireMessageQueue(IBackgroundJobClient backgroundJobClient) : IMessageQueue
{
    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to, nameof(to));
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));

        EmailMessage message = new(to, subject, textBody, htmlBody);
        backgroundJobClient.Enqueue<MessageProcessor>(processor => processor.ProcessMessage(message));
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber, nameof(phoneNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));

        SmsMessage message = new(phoneNumber, code);
        backgroundJobClient.Enqueue<MessageProcessor>(processor => processor.ProcessMessage(message));
    }
}
