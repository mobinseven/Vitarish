using FluentValidation;
using Vitarish.AuthenticationClients.Application.Commands;

namespace Vitarish.AuthenticationClients.Infrastructure.Validators;

/// <summary>
/// Validator for <see cref="DeleteAuthenticationClient"/>.
/// </summary>
public class DeleteAuthenticationClientValidator : AbstractValidator<DeleteAuthenticationClient>
{
    public DeleteAuthenticationClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
