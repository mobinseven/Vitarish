
using FluentValidation;

namespace Vitarish.AuthenticationClients.Application.Queries;

/// <summary>
/// Validator for <see cref="GetAuthenticationClient"/>.
/// </summary>
public sealed class GetAuthenticationClientValidator : AbstractValidator<GetAuthenticationClient>
{
    public GetAuthenticationClientValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
