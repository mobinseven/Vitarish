using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query for a single <see cref="User"/> by identifier.
/// </summary>
public sealed record GetUser(string Id) : IRequest<User?>;

/// <summary>
/// Validator for <see cref="GetUser"/>.
/// </summary>
public sealed class GetUserValidator : AbstractValidator<GetUser>
{
    public GetUserValidator()
    {
        RuleFor(q => q.Id).NotEmpty();
    }
}
