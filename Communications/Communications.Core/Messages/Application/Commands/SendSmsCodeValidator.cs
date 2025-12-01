using FluentValidation;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Validator for <see cref="SendSmsCode"/>.
/// </summary>
public sealed class SendSmsCodeValidator : AbstractValidator<SendSmsCode>
{
    public SendSmsCodeValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}
