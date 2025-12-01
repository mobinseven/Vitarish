using FluentValidation;
using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Communications.Core.Messages.Domain.Models;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an SMS message.
/// </summary>
public sealed record SendSms(string Recipient, string Body) : IRequest<Result<SmsSendResult>>;

/// <summary>
/// Validator for <see cref="SendSms"/>.
/// </summary>
public sealed class SendSmsValidator : AbstractValidator<SendSms>
{
    public SendSmsValidator()
    {
        _ = RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required");

        _ = RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Body is required");
    }
}
