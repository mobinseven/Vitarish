namespace Vorn.Caas.Messages.Application;

using System;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Caas.Messages.Domain.Interfaces;
using Vorn.Caas.Messages.Domain.Models;

public static class SendSms
{
    public static async Task<SmsSendResult> ExecuteAsync(
        IMessagesService service,
        Message message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(message);

        Validate(message);
        cancellationToken.ThrowIfCancellationRequested();

        return await service.SendSms(message, cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            throw new ArgumentException("Recipient is required");
        }

        if (string.IsNullOrWhiteSpace(message.Body))
        {
            throw new ArgumentException("Body is required");
        }
    }
}
