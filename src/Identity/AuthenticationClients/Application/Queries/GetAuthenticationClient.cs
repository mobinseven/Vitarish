
using FluentValidation;
using MediatR;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="AuthenticationClient"/> by identifier.
/// </summary>
public sealed record GetAuthenticationClient(string Id) : IRequest<AuthenticationClient?>;

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
