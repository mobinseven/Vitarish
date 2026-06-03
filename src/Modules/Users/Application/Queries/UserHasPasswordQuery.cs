using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record UserHasPasswordQuery(string Id) : IRequest<bool>;

public class UserHasPasswordQueryValidator : AbstractValidator<UserHasPasswordQuery>
{
    public UserHasPasswordQueryValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
