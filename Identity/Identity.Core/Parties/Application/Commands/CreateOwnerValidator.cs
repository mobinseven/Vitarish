using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="CreateOwner"/>.
/// </summary>
public sealed class CreateOwnerValidator : AbstractValidator<CreateOwner>
{
    public CreateOwnerValidator()
    {
        _ = RuleFor(c => c.Name).NotEmpty();
    }
}
