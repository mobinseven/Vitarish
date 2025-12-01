using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="ActivateTenantIndicator"/>.
/// </summary>
public sealed class ActivateTenantIndicatorValidator : AbstractValidator<ActivateTenantIndicator>
{
    public ActivateTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.TenantId).NotEmpty();
        _ = RuleFor(c => c.IndicatorId).NotEmpty();
    }
}
