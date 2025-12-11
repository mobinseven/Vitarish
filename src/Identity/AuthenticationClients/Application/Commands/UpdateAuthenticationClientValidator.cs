using FluentValidation;

namespace Vitarish.AuthenticationClients.Application.Commands;

/// <summary>
/// Validator for <see cref="UpdateAuthenticationClient"/>.
/// </summary>
public class UpdateAuthenticationClientValidator : AbstractValidator<UpdateAuthenticationClient>
{
    public UpdateAuthenticationClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Url).NotEmpty();
    }
}
