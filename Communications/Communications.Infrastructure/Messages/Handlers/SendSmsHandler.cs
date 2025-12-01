namespace Vitarish.Communications.Infrastructure.Messages.Handlers;

using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Application.Commands;
using DomainModels = Vitarish.Communications.Core.Messages.Domain.Models;
using ServiceModels = Vitarish.Communications.Core.Messages.Models;

internal sealed class SendSmsHandler(IMessagesService messagesService)
    : IRequestHandler<SendSms, Result<DomainModels.SmsSendResult>>
{
    public async Task<Result<DomainModels.SmsSendResult>> Handle(SendSms request, CancellationToken cancellationToken)
    {
        var message = new ServiceModels.Message
        {
            Recipient = request.Recipient ?? string.Empty,
            Body = request.Body ?? string.Empty
        };

        var result = await messagesService.SendSmsAsync(message, cancellationToken);
        
        // Map from service model to domain model
        var domainResult = new DomainModels.SmsSendResult(
            MessageId: result.MessageId,
            Recipient: result.Recipient,
            Cost: result.Cost,
            PackId: result.PackId,
            SubmittedAt: result.SubmittedAt,
            RelatedMessageIds: result.RelatedMessageIds,
            Metadata: result.Metadata);
        
        return Result.Success(domainResult);
    }
}
