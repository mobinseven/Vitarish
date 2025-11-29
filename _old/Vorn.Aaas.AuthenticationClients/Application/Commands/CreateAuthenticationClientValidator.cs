using FluentValidation;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

/// <summary>
/// Validator for <see cref="CreateAuthenticationClient"/>.
/// </summary>
public class CreateAuthenticationClientValidator : AbstractValidator<CreateAuthenticationClient>
{
    public CreateAuthenticationClientValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Url).NotEmpty();
    }
}
