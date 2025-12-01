using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateOwner"/>.
/// </summary>
public sealed class UpdateOwnerValidator : AbstractValidator<UpdateOwner>
{
    public UpdateOwnerValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Name).NotEmpty();
    }
}
