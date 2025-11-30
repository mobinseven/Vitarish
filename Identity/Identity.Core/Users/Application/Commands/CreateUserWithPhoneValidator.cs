using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="CreateUserWithPhone"/>.
/// </summary>
public sealed class CreateUserWithPhoneValidator : AbstractValidator<CreateUserWithPhone>
{
    public CreateUserWithPhoneValidator()
    {
        RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}
