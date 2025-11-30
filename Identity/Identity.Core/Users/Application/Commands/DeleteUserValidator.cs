using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="DeleteUser"/>.
/// </summary>
public sealed class DeleteUserValidator : AbstractValidator<DeleteUser>
{
    public DeleteUserValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
