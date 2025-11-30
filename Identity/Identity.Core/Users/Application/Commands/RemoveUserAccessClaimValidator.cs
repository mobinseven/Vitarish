using FluentValidation;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Validator for <see cref="RemoveUserAccessClaim"/>.
/// </summary>
public sealed class RemoveUserAccessClaimValidator : AbstractValidator<RemoveUserAccessClaim>
{
    public RemoveUserAccessClaimValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty();
    }
}
