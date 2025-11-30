using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="SetUserPhoneNumber"/>.
/// </summary>
public sealed class SetUserPhoneNumberValidator : AbstractValidator<SetUserPhoneNumber>
{
    public SetUserPhoneNumberValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}
