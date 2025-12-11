using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Abstractions;

public interface IMessagesService
{
    Task SendEmail(Message message, CancellationToken cancellationToken = default);
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendSmsCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
