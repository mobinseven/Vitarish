using FluentValidation;
using MediatR;
using Vitarish.Identity.Contracts.Users;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query to get all users with access to a specific client.
/// </summary>
public sealed record GetClientUsers(string ClientId) : IRequest<IEnumerable<UserDto>>;

/// <summary>
/// Validator for <see cref="GetClientUsers"/>.
/// </summary>
public sealed class GetClientUsersValidator : AbstractValidator<GetClientUsers>
{
    public GetClientUsersValidator()
    {
        RuleFor(q => q.ClientId).NotEmpty();
    }
}
