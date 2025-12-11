using Vitarish.Clients.Modules.Communications.Abstractions.Models;

namespace Vitarish.Clients.Modules.Communications.Abstractions;

public interface IMessageClient
{
    Task SendEmailAsync(string recipient, string? subject, string? body,
        string? htmlBody = null, string? sender = null, CancellationToken cancellationToken = default);
    Task<SmsSendResult> SendSmsAsync(string phone, string body, string? sender = null, CancellationToken cancellationToken = default);
    Task SendSmsCodeAsync(string phone, string code, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
