using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateTenant"/>.
/// </summary>
public sealed class UpdateTenantValidator : AbstractValidator<UpdateTenant>
{
    public UpdateTenantValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Name).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
