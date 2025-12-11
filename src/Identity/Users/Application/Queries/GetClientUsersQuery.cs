using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Queries;

public record GetClientUsersQuery(string ClientId) : IRequest<IEnumerable<UserDto>>;

public class GetClientUsersQueryValidator : AbstractValidator<GetClientUsersQuery>
{
    public GetClientUsersQueryValidator()
    {
        _ = RuleFor(q => q.ClientId).NotEmpty();
    }
}
