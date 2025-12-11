using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record HasUserAccessQuery(string UserId, string ClientId) : IRequest<bool>;

public class HasUserAccessQueryValidator : AbstractValidator<HasUserAccessQuery>
{
    public HasUserAccessQueryValidator()
    {
        _ = RuleFor(q => q.UserId).NotEmpty();
        _ = RuleFor(q => q.ClientId).NotEmpty();
    }
}
