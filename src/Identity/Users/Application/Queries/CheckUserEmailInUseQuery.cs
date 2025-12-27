using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record CheckUserEmailInUseQuery(string Email, string? ExcludeUserId) : IRequest<bool>;

public class CheckUserEmailInUseQueryValidator : AbstractValidator<CheckUserEmailInUseQuery>
{
    public CheckUserEmailInUseQueryValidator()
    {
        _ = RuleFor(q => q.Email).NotEmpty();
    }
}
