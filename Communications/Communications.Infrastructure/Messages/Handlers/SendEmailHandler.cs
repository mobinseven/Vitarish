namespace Vitarish.Communications.Infrastructure.Messages.Handlers;

using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Application.Commands;
using ServiceModels = Vitarish.Communications.Core.Messages.Models;

internal sealed class SendEmailHandler(IMessagesService messagesService)
    : IRequestHandler<SendEmail, Result>
{
    public async Task<Result> Handle(SendEmail request, CancellationToken cancellationToken)
    {
        var message = new ServiceModels.Message
        {
            Recipient = request.Recipient ?? string.Empty,
            Subject = request.Subject,
            Body = request.Body,
            HtmlBody = request.HtmlBody
        };

        await messagesService.SendEmailAsync(message, cancellationToken);
        return Result.Success();
    }
}
