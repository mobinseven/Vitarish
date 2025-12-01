using FluentValidation;

namespace Vitarish.Identity.Core.Parties.Application.Commands;

/// <summary>
/// Validator for <see cref="AddTenantIndicator"/>.
/// </summary>
public sealed class AddTenantIndicatorValidator : AbstractValidator<AddTenantIndicator>
{
    public AddTenantIndicatorValidator()
    {
        _ = RuleFor(c => c.Value)
            .NotEmpty()
            .MaximumLength(253)
            .Must(BeValidHostname).WithMessage("Value must be a valid hostname");
        _ = RuleFor(c => c.Method).IsInEnum();
    }

    private static bool BeValidHostname(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Uri.CheckHostName(value) != UriHostNameType.Unknown;
}
