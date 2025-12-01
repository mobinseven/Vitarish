using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="DeleteTenantIndicator"/>.
/// </summary>
public sealed class DeleteTenantIndicatorValidator : AbstractValidator<DeleteTenantIndicator>
{
    public DeleteTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.TenantId).NotEmpty();
        _ = RuleFor(c => c.IndicatorId).NotEmpty();
    }
}
