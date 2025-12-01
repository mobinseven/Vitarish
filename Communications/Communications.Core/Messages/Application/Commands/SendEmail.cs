using FluentValidation;
using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an email message.
/// </summary>
public sealed record SendEmail(
    string Recipient,
    string? Subject,
    string? Body,
    string? HtmlBody) : IRequest<Result>;

/// <summary>
/// Validator for <see cref="SendEmail"/>.
/// </summary>
public sealed class SendEmailValidator : AbstractValidator<SendEmail>
{
    public SendEmailValidator()
    {
        _ = RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required");
    }
}
