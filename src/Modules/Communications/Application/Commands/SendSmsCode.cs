using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Commands;

public static class SendSmsCode
{
    public static async Task ExecuteAsync(
        IMessagesService service,
        Message message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(message);

        Validate(message);
        cancellationToken.ThrowIfCancellationRequested();

        await service.SendSmsCode(new Message
        {
            Recipient = message.Recipient,
            Body = message.Body
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            throw new ArgumentException("Phone is required");
        }

        if (string.IsNullOrWhiteSpace(message.Body))
        {
            throw new ArgumentException("Code is required");
        }
    }
}
