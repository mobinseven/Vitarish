using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class RemoveUserFromRoleCommandHandler(
    UsersDbContext dbContext,
    IRequestContext requestContext)
    : IRequestHandler<RemoveUserFromRoleCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(
        RemoveUserFromRoleCommand request,
        CancellationToken cancellationToken)
    {
        string roleName = RoleForTenant(request.RoleName, request.TenantId);

        var role = await dbContext.Roles
            .Where(r => r.Name == roleName)
            .Select(r => new { r.Id })
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = $"Role '{roleName}' does not exist."
                });
        }

        IdentityUserRole<string>? userRole = await dbContext.UserRoles
            .SingleOrDefaultAsync(
                ur => ur.UserId == request.User.Id && ur.RoleId == role.Id,
                cancellationToken);

        if (userRole is null)
        {
            // User is not in the role → Identity treats this as success
            return IdentityResult.Success;
        }

        _ = dbContext.UserRoles.Remove(userRole);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return IdentityResult.Success;
    }

    private string RoleForTenant(string roleName, Guid? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId?.ToString() ?? requestContext.TenantId)
            ? roleName
            : $"{tenantId?.ToString() ?? requestContext.TenantId}:{roleName}";
}
