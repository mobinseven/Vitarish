using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="SetUserPhoneNumber"/>.
/// </summary>
public sealed class SetUserPhoneNumberValidator : AbstractValidator<SetUserPhoneNumber>
{
    public SetUserPhoneNumberValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}
