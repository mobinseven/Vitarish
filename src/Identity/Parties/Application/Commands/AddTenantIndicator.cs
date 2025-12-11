using FluentValidation;
using MediatR;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to add a tenant indicator.
/// </summary>
public sealed record AddTenantIndicator(
    Guid TenantId,
    IndicatorMethod Method,
    string Value,
    bool IsPrimary = false,
    IndicatorVerificationMethod VerificationMethod = IndicatorVerificationMethod.DnsTxt) : IRequest<TenantIndicator?>;

/// <summary>
/// Validator for <see cref="AddTenantIndicator"/>.
/// </summary>
public class AddTenantIndicatorValidator : AbstractValidator<AddTenantIndicator>
{
    public AddTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.Value)
            .NotEmpty()
            .MaximumLength(253)
            .Must(BeValidHostname).WithMessage("Value must be a valid hostname");
        _ = RuleFor(c => c.Method).IsInEnum();
    }

    private bool BeValidHostname(string value) =>
        // Basic hostname validation
        !string.IsNullOrWhiteSpace(value) &&
               Uri.CheckHostName(value) != UriHostNameType.Unknown;
}
