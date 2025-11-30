using FluentValidation;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

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
