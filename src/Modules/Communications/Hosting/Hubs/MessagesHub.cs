using Microsoft.AspNetCore.SignalR;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Hosting.Hubs;

public sealed class MessagesHub(IMessagesService messagesService) : Hub
{
    public Task SendEmailAsync(Message message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return messagesService.SendEmail(message, cancellationToken);
    }

    public Task<SmsSendResult> SendSmsAsync(Message message, CancellationToken cancellationToken) =>
        messagesService.SendSms(message, cancellationToken);

    public Task SendSmsCodeAsync(Message message, CancellationToken cancellationToken) =>
        messagesService.SendSmsCode(message, cancellationToken);

    public Task<SmsDeliveryState> GetSmsDeliveryStateAsync(long messageId, CancellationToken cancellationToken) =>
        messagesService.GetSmsDeliveryStateAsync(messageId, cancellationToken);
}
