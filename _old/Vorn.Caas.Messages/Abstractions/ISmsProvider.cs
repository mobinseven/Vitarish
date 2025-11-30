namespace Vorn.Communications.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Vorn.Communications.Models;

public interface ISmsProvider
{
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
