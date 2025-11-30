using FluentValidation;
using MediatR;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query for a single <see cref="User"/> by username.
/// </summary>
public sealed record GetUserByName(string UserName) : IRequest<User?>;

/// <summary>
/// Validator for <see cref="GetUserByName"/>.
/// </summary>
public sealed class GetUserByNameValidator : AbstractValidator<GetUserByName>
{
    public GetUserByNameValidator()
    {
        _ = RuleFor(q => q.UserName).NotEmpty();
    }
}
