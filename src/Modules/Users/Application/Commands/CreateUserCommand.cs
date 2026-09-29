using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record CreateUserCommand(User User) : IRequest<IdentityResult>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            if (command.User is null)
                await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
        });
    }
}
