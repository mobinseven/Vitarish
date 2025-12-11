namespace Vitarish.Interface.Infrastructure.Services;

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
