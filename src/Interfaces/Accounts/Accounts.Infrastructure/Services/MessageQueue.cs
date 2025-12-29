namespace Vitarish.Accounts.Infrastructure.Services;

/// <summary>
/// Abstraction for enqueueing messages to be processed asynchronously.
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Enqueues an email message to be sent.
    /// </summary>
    void EnqueueEmail(string to, string subject, string textBody, string htmlBody);

    /// <summary>
    /// Enqueues an SMS message to be sent.
    /// </summary>
    void EnqueueSms(string phoneNumber, string code);
}

public abstract record MessageQueueMessage;

public sealed record EmailQueueMessage(
    string To,
    string Subject,
    string TextBody,
    string HtmlBody) : MessageQueueMessage;

public sealed record SmsQueueMessage(
    string PhoneNumber,
    string Code) : MessageQueueMessage;

public sealed class MessageQueue : IMessageQueue
{
    private readonly Queue<MessageQueueMessage> messages = new();

    public int Count => messages.Count;

    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
        messages.Enqueue(new EmailQueueMessage(to, subject, textBody, htmlBody));
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
        messages.Enqueue(new SmsQueueMessage(phoneNumber, code));
    }

    public MessageQueueMessage Dequeue() => messages.Dequeue();
}
