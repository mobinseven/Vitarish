using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public sealed record ReplaceTenantRoleCommand(string UserId, Guid TenantId, string? RoleSuffix) : IRequest<IdentityResult>;

public sealed class ReplaceTenantRoleCommandValidator : AbstractValidator<ReplaceTenantRoleCommand>
{
    public ReplaceTenantRoleCommandValidator()
    {
        _ = RuleFor(command => command.UserId).NotEmpty();
        _ = RuleFor(command => command.TenantId).NotEqual(Guid.Empty);
    }
}
