using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class RemoveUserFromRoleCommandHandler(
    UserManager<User> userManager,
    IRequestContext requestContext) : IRequestHandler<RemoveUserFromRoleCommand, IdentityResult>
{
    public Task<IdentityResult> Handle(RemoveUserFromRoleCommand request, CancellationToken cancellationToken)
    {
        string roleName = RoleForTenant(request.RoleName);
        return userManager.RemoveFromRoleAsync(request.User, roleName);
    }

    private string RoleForTenant(string roleName) =>
        string.IsNullOrWhiteSpace(requestContext.TenantId) ? roleName : $"{requestContext.TenantId}:{roleName}";
}
