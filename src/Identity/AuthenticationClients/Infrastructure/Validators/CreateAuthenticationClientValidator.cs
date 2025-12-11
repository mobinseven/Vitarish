using FluentValidation;
using Vitarish.AuthenticationClients.Application.Commands;

namespace Vitarish.AuthenticationClients.Infrastructure.Validators;

/// <summary>
/// Validator for <see cref="CreateAuthenticationClient"/>.
/// </summary>
public class CreateAuthenticationClientValidator : AbstractValidator<CreateAuthenticationClient>
{
    public CreateAuthenticationClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Url).NotEmpty();
    }
}
