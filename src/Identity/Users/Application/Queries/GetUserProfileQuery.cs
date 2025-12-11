using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record GetUserProfileQuery(string UserId) : IRequest<Dictionary<string, string>>;

public class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        _ = RuleFor(q => q.UserId).NotEmpty();
    }
}
