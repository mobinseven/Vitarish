using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="DisableTenantIndicator"/>.
/// </summary>
public sealed class DisableTenantIndicatorValidator : AbstractValidator<DisableTenantIndicator>
{
    public DisableTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.TenantId).NotEmpty();
        _ = RuleFor(c => c.IndicatorId).NotEmpty();
    }
}
