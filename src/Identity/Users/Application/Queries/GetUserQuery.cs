
using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUserQuery(string Id) : IRequest<User?>;

public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
