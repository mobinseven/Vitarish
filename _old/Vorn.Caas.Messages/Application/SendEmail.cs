namespace Vorn.Caas.Messages.Application;

using Vorn.Caas.Messages.Domain.Interfaces;
using Vorn.Caas.Messages.Domain.Models;

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
