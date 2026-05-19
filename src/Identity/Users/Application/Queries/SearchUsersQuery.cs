using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record SearchUsersQuery(string Query, int MaxResults) : IRequest<IEnumerable<User>>;

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        _ = RuleFor(q => q.Query).NotNull().MaximumLength(200);
        _ = RuleFor(q => q.MaxResults).InclusiveBetween(1, 200);
    }
}
