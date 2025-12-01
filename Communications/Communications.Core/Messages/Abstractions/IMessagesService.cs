namespace Vitarish.Communications.Core.Messages.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Vitarish.Communications.Core.Messages.Models;

public interface IMessagesService
{
    Task SendEmailAsync(Message message, CancellationToken cancellationToken = default);
    Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken = default);
    Task SendSmsCodeAsync(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
