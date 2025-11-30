using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="SetUserAccessClaim"/>.
/// </summary>
public sealed class SetUserAccessClaimValidator : AbstractValidator<SetUserAccessClaim>
{
    public SetUserAccessClaimValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
        RuleFor(c => c.AccessValue).NotEmpty();
    }
}
