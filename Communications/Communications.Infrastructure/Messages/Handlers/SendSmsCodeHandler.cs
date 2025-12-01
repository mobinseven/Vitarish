namespace Vitarish.Communications.Infrastructure.Messages.Handlers;

using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Application.Commands;
using ServiceModels = Vitarish.Communications.Core.Messages.Models;

internal sealed class SendSmsCodeHandler(IMessagesService messagesService)
    : IRequestHandler<SendSmsCode, Result>
{
    public async Task<Result> Handle(SendSmsCode request, CancellationToken cancellationToken)
    {
        var message = new ServiceModels.Message
        {
            Recipient = request.Recipient ?? string.Empty,
            Body = request.Code ?? string.Empty
        };

        await messagesService.SendSmsCodeAsync(message, cancellationToken);
        return Result.Success();
    }
}
