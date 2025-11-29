namespace Vitarish.Communications.Core.Services;

using Vitarish.Communications.Core.Domain.Models;

/// <summary>
/// Service interface for sending messages via email and SMS.
/// </summary>
public interface IMessagesService
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the SMS send operation.</returns>
    Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS verification code.
    /// </summary>
    /// <param name="message">The message containing the verification code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSmsCodeAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the delivery state of an SMS message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivery state of the message.</returns>
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
