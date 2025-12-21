using FluentValidation;
using MediatR;
using Vitarish.Requests.Domain.Models;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Queries;

/// <summary>
/// Query to get access claim for a request based on the full request context.
/// This replaces the client-centric GetUserAccessClaimQuery.
/// </summary>
public record GetRequestAccessClaimQuery(RequestIdentityDto Request) : IRequest<ClaimDto?>;

public class GetRequestAccessClaimQueryValidator : AbstractValidator<GetRequestAccessClaimQuery>
{
    public GetRequestAccessClaimQueryValidator()
    {
        _ = RuleFor(q => q.Request).NotNull();
        _ = RuleFor(q => q.Request.UserId)
            .NotEmpty()
            .When(q => q.Request != null && q.Request.UserId != null);
        _ = RuleFor(q => q.Request.ClientId)
            .NotEmpty()
            .When(q => q.Request != null && q.Request.ClientId != null);
    }
}
