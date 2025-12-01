namespace Vitarish.Communications.Infrastructure.Messages.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Application.Queries;
using DomainModels = Vitarish.Communications.Core.Messages.Domain.Models;
using ServiceModels = Vitarish.Communications.Core.Messages.Models;

internal sealed class GetSmsDeliveryStateHandler(
    IMessagesService messagesService,
    ILogger<GetSmsDeliveryStateHandler> logger)
    : IRequestHandler<GetSmsDeliveryState, DomainModels.SmsDeliveryState?>
{
    public async Task<DomainModels.SmsDeliveryState?> Handle(GetSmsDeliveryState request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await messagesService.GetSmsDeliveryStateAsync(request.MessageId, cancellationToken);
            
            if (result is null)
            {
                return null;
            }
            
            return new DomainModels.SmsDeliveryState(
                MessageId: result.MessageId,
                Recipient: result.Recipient,
                StatusCode: result.StatusCode,
                Status: result.Status,
                MessageText: result.MessageText,
                Cost: result.Cost,
                SentAt: result.SentAt,
                DeliveredAt: result.DeliveredAt,
                Metadata: result.Metadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving SMS delivery state for message ID {MessageId}", request.MessageId);
            return null;
        }
    }
}
