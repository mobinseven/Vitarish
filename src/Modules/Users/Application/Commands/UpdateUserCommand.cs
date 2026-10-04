using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record UpdateUserCommand(User User) : IRequest<IdentityResult>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = When(c => c.User is not null, () =>
        {
            _ = RuleFor(c => c.User.Id).NotNull();
        });
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            if (command.User is null || command.User.Id is null)
                await managementAudit.AccountEditFailedAsync(command.User?.Id, null, cancellationToken);
        });
    }
}
