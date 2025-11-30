using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="SetUserEmail"/>.
/// </summary>
public sealed class SetUserEmailValidator : AbstractValidator<SetUserEmail>
{
    public SetUserEmailValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
