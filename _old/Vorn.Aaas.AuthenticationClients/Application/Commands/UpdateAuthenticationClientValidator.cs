using FluentValidation;

namespace Vorn.Aaas.AuthenticationClients.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateAuthenticationClient"/>.
/// </summary>
public class UpdateAuthenticationClientValidator : AbstractValidator<UpdateAuthenticationClient>
{
    public UpdateAuthenticationClientValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Url).NotEmpty();
    }
}
