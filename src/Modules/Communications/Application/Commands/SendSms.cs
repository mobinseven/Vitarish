using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Commands;

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
