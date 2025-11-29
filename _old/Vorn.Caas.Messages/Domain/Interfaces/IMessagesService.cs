namespace Vorn.Caas.Messages.Domain.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Vorn.Caas.Messages.Domain.Models;

public interface IMessagesService
{
    Task SendEmail(Message message);
    Task<SmsSendResult> SendSms(Message message, CancellationToken cancellationToken = default);
    Task SendSmsCode(Message message, CancellationToken cancellationToken = default);
    Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken = default);
}
