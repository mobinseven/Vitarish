using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="SetUserAccessClaim"/>.
/// </summary>
public sealed class SetUserAccessClaimValidator : AbstractValidator<SetUserAccessClaim>
{
    public SetUserAccessClaimValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
