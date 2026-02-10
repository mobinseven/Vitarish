using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Abstractions;

public interface IMessagesService
{
    Task SendEmail(Message message, CancellationToken cancellationToken = default);
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendSmsCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
    Task SendSmsReminder(string receipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default);
}
