using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Assigns the platform administrator role to <paramref name="UserId"/> only while
/// no account holds it. Sent by sign-in after the user's contact code verifies;
/// returns whether this call made the assignment.
/// </summary>
public record GrantInitialPlatformAdministratorCommand(string UserId) : IRequest<bool>;

public sealed class GrantInitialPlatformAdministratorCommandValidator : AbstractValidator<GrantInitialPlatformAdministratorCommand>
{
    public GrantInitialPlatformAdministratorCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
    }
}
