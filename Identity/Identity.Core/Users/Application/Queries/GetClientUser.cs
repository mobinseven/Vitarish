using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query to get a specific user with access to a client.
/// </summary>
public sealed record GetClientUser(string ClientId, string UserId) : IRequest<User?>;

/// <summary>
/// Validator for <see cref="GetClientUser"/>.
/// </summary>
public sealed class GetClientUserValidator : AbstractValidator<GetClientUser>
{
    public GetClientUserValidator()
    {
        RuleFor(q => q.ClientId).NotEmpty();
        RuleFor(q => q.UserId).NotEmpty();
    }
}
