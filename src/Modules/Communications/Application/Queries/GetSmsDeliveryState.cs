using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Application.Queries;

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
