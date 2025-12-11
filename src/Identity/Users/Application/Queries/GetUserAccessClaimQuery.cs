using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Queries;

public record GetUserAccessClaimQuery(string UserId, string ClientId) : IRequest<ClaimDto?>;

public class GetUserAccessClaimQueryValidator : AbstractValidator<GetUserAccessClaimQuery>
{
    public GetUserAccessClaimQueryValidator()
    {
        _ = RuleFor(q => q.UserId).NotEmpty();
        _ = RuleFor(q => q.ClientId).NotEmpty();
    }
}
