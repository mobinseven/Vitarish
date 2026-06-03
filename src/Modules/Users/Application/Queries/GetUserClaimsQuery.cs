using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Queries;

public record GetUserClaimsQuery(string UserId) : IRequest<IEnumerable<ClaimDto>>;

public class GetUserClaimsQueryValidator : AbstractValidator<GetUserClaimsQuery>
{
    public GetUserClaimsQueryValidator()
    {
        _ = RuleFor(q => q.UserId).NotEmpty();
    }
}
