using FluentValidation;
using MediatR;

namespace Vitarish.Identity.Core.Users.Application.Queries;

/// <summary>
/// Query to check if user has access to a specific client.
/// </summary>
public sealed record HasUserAccess(string UserId, string ClientId) : IRequest<bool>;

/// <summary>
/// Validator for <see cref="HasUserAccess"/>.
/// </summary>
public sealed class HasUserAccessValidator : AbstractValidator<HasUserAccess>
{
    public HasUserAccessValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ClientId).NotEmpty();
    }
}
