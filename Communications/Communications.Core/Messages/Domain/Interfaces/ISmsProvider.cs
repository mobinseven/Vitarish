namespace Vitarish.Communications.Core.Messages.Domain.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Vitarish.Communications.Core.Messages.Domain.Models;

/// <summary>
/// Provider interface for SMS-specific operations.
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a verification code via SMS.
    /// </summary>
    Task SendCode(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the delivery state of an SMS message.
    /// </summary>
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
