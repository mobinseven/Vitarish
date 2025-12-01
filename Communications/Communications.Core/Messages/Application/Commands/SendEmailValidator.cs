using FluentValidation;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Validator for <see cref="SendEmail"/>.
/// </summary>
public sealed class SendEmailValidator : AbstractValidator<SendEmail>
{
    public SendEmailValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty();
    }
}
