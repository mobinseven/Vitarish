
using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUserByNameQuery(string UserName) : IRequest<User?>;

public class GetUserByNameQueryValidator : AbstractValidator<GetUserByNameQuery>
{
    public GetUserByNameQueryValidator()
    {
        _ = RuleFor(q => q.UserName).NotEmpty();
    }
}
