namespace Vitarish.Communications.Core.Messages.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Vitarish.Communications.Core.Messages.Models;

public interface ISmsProvider
{
    Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken = default);
    Task SendCodeAsync(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
