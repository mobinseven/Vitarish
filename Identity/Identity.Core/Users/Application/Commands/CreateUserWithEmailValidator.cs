using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="CreateUserWithEmail"/>.
/// </summary>
public sealed class CreateUserWithEmailValidator : AbstractValidator<CreateUserWithEmail>
{
    public CreateUserWithEmailValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
