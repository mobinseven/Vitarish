using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="CreateTenant"/>.
/// </summary>
public sealed class CreateTenantValidator : AbstractValidator<CreateTenant>
{
    public CreateTenantValidator()
    {
        _ = RuleFor(c => c.OwnerId).NotEmpty();
        _ = RuleFor(c => c.Name).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
