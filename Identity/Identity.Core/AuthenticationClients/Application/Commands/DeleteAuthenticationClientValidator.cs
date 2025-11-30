using FluentValidation;

namespace Vitarish.Identity.Core.AuthenticationClients.Application.Commands;

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
