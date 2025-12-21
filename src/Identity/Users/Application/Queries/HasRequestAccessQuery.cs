using FluentValidation;
using MediatR;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Users.Application.Queries;

/// <summary>
/// Query to check if a request has access based on the full request context.
/// This replaces the client-centric HasUserAccessQuery.
/// </summary>
public record HasRequestAccessQuery(RequestIdentityDto Request) : IRequest<bool>;

public class HasRequestAccessQueryValidator : AbstractValidator<HasRequestAccessQuery>
{
    public HasRequestAccessQueryValidator()
    {
        _ = RuleFor(q => q.Request).NotNull();
        _ = RuleFor(q => q.Request.UserId).NotEmpty();
        _ = RuleFor(q => q.Request.ClientId).NotEmpty();
    }
}
