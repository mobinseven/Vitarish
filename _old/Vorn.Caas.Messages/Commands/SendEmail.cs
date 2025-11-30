namespace Vorn.Communications.Commands;

using Vorn.Communications.Abstractions;
using Vorn.Communications.Models;

public static class SendEmail
{
    public static async Task ExecuteAsync(IMessagesService service, Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(message);

        Validate(message);
        cancellationToken.ThrowIfCancellationRequested();

        await service.SendEmail(message);
    }

    private static void Validate(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Recipient))
        {
            throw new ArgumentException("Recipient is required");
        }
    }
}
