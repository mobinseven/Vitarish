
using FluentValidation;
using Vitarish.ApiClients.Application.Commands;

namespace Vitarish.ApiClients.Infrastructure.Validators;

/// <summary>
/// Validator for <see cref="CreateApiClient"/>.
/// </summary>
public class CreateApiClientValidator : AbstractValidator<CreateApiClient>
{
    public CreateApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Secret).NotEmpty();
        _ = RuleFor(c => c.Scopes).NotEmpty();
        _ = RuleFor(c => c.HostUri).NotEmpty();
    }
}
