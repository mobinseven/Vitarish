namespace Vitarish.Communications.Application.Abstractions;

public interface ISmsProvider
{
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
    Task SendReminder(string recipient, string? project = null, string? procedure = null, string? evnt = null, string? tenant = null, CancellationToken cancellationToken = default);
    Task SendTemplatedSms(string recipient, string templateId, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default);
}
