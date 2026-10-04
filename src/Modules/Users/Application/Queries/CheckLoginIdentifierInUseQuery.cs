using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public enum LoginIdentifierKind
{
    Email,
    Phone
}

public record CheckLoginIdentifierInUseQuery(string Value, LoginIdentifierKind Kind, string? ExcludeUserId) : IRequest<bool>;

public class CheckLoginIdentifierInUseQueryValidator : AbstractValidator<CheckLoginIdentifierInUseQuery>
{
    public CheckLoginIdentifierInUseQueryValidator()
    {
        _ = RuleFor(query => query.Value).NotEmpty();
        _ = RuleFor(query => query.Kind).IsInEnum();
    }
}
