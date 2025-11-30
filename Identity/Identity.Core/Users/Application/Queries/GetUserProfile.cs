using FluentValidation;
using MediatR;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query to get a user's profile preferences.
/// </summary>
public sealed record GetUserProfile(string UserId) : IRequest<Dictionary<string, string>>;

/// <summary>
/// Validator for <see cref="GetUserProfile"/>.
/// </summary>
public sealed class GetUserProfileValidator : AbstractValidator<GetUserProfile>
{
    public GetUserProfileValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
    }
}
