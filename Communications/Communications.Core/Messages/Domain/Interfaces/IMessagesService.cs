namespace Vitarish.Communications.Core.Messages.Domain.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Vitarish.Communications.Core.Messages.Domain.Models;

/// <summary>
/// Service interface for sending messages via email and SMS.
/// </summary>
public interface IMessagesService
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    Task SendEmail(Message message);

    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS verification code.
    /// </summary>
    Task SendSmsCode(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the delivery state of an SMS message.
    /// </summary>
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
