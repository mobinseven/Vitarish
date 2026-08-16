using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record RemoveUserFromRoleCommand(User User, string RoleName, Guid? TenantId = null) : IRequest<IdentityResult>;

public sealed class RemoveUserFromRoleCommandValidator : AbstractValidator<RemoveUserFromRoleCommand>
{
    public RemoveUserFromRoleCommandValidator()
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = RuleFor(c => c.RoleName).NotEmpty();
    }
}
