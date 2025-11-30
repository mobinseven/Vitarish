using FluentValidation;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

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
