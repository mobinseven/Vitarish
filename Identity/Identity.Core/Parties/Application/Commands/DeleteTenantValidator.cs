using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="DeleteTenant"/>.
/// </summary>
public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenant>
{
    public DeleteTenantValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
