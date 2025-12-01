using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="VerifyTenantIndicator"/>.
/// </summary>
public sealed class VerifyTenantIndicatorValidator : AbstractValidator<VerifyTenantIndicator>
{
    public VerifyTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.TenantId).NotEmpty();
        _ = RuleFor(c => c.IndicatorId).NotEmpty();
    }
}
