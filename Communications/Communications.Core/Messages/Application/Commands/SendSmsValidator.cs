using FluentValidation;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Validator for <see cref="SendSms"/>.
/// </summary>
public sealed class SendSmsValidator : AbstractValidator<SendSms>
{
    public SendSmsValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}
