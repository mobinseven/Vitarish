using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record CheckUserPasswordQuery(string Id, string Password) : IRequest<bool>;

public class CheckUserPasswordQueryValidator : AbstractValidator<CheckUserPasswordQuery>
{
    public CheckUserPasswordQueryValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
        _ = RuleFor(q => q.Password).NotEmpty();
    }
}
