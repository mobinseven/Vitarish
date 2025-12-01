using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="DeleteOwner"/>.
/// </summary>
public sealed class DeleteOwnerValidator : AbstractValidator<DeleteOwner>
{
    public DeleteOwnerValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
