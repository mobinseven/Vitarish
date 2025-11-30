using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="RemoveUserAccessClaim"/>.
/// </summary>
public sealed class RemoveUserAccessClaimValidator : AbstractValidator<RemoveUserAccessClaim>
{
    public RemoveUserAccessClaimValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
