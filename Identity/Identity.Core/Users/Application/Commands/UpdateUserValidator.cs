using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateUser"/>.
/// </summary>
public sealed class UpdateUserValidator : AbstractValidator<UpdateUser>
{
    public UpdateUserValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
