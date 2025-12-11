using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Abstractions;

public interface ISmsProvider
{
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
