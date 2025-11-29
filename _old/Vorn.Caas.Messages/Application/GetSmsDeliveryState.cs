namespace Vorn.Caas.Messages.Application;

using System;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Caas.Messages.Domain.Interfaces;
using Vorn.Caas.Messages.Domain.Models;

public static class GetSmsDeliveryState
{
    public static async Task<SmsDeliveryState> ExecuteAsync(
        IMessagesService service,
        long messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (messageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(messageId), "Message ID must be a positive value.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await service.GetSmsDeliveryStateAsync(messageId, cancellationToken).ConfigureAwait(false);
    }
}
