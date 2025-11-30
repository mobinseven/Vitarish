using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateUserProfile"/>.
/// </summary>
public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfile>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Preferences).NotNull();
    }
}
