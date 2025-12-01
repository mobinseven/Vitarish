using FluentValidation;
using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an SMS verification code.
/// </summary>
public sealed record SendSmsCode(string Recipient, string Code) : IRequest<Result>;

/// <summary>
/// Validator for <see cref="SendSmsCode"/>.
/// </summary>
public sealed class SendSmsCodeValidator : AbstractValidator<SendSmsCode>
{
    public SendSmsCodeValidator()
    {
        _ = RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Phone is required");

        _ = RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required");
    }
}
