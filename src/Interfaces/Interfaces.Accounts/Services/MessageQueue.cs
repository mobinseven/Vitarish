namespace Vitarish.Interfaces.Accounts.Services;

/// <summary>
/// Abstraction for enqueueing messages to be processed asynchronously.
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Gets the current number of queued messages.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Enqueues an email message to be sent.
    /// </summary>
    void EnqueueEmail(string to, string subject, string textBody, string htmlBody);

    /// <summary>
    /// Enqueues an SMS message to be sent.
    /// </summary>
    void EnqueueSms(string phoneNumber, string code);

    /// <summary>
    /// Dequeues the next message to be processed.
    /// </summary>
    MessageQueueMessage Dequeue();
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
    private readonly System.Collections.Concurrent.ConcurrentQueue<MessageQueueMessage> messages = new();

    public int Count => messages.Count;

    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(textBody);
        ArgumentNullException.ThrowIfNull(htmlBody);

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Email recipient address cannot be empty.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Email subject cannot be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(textBody))
        {
            throw new ArgumentException("Email text body cannot be empty.", nameof(textBody));
        }

        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException("Email HTML body cannot be empty.", nameof(htmlBody));
        }

        messages.Enqueue(new EmailQueueMessage(to, subject, textBody, htmlBody));
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be empty.", nameof(code));
        }

        messages.Enqueue(new SmsQueueMessage(phoneNumber, code));
    }

    public MessageQueueMessage Dequeue()
    {
        if (messages.TryDequeue(out MessageQueueMessage? message))
        {
            return message;
        }

        throw new InvalidOperationException("The queue is empty.");
    }
}
