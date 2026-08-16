using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class AddUserToRoleCommandHandler(
    UsersDbContext dbContext,
    IRequestContext requestContext) : IRequestHandler<AddUserToRoleCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(AddUserToRoleCommand request, CancellationToken cancellationToken)
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

        bool assigned = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == request.User.Id && ur.RoleId == role.Id, cancellationToken);
        if (assigned)
        {
            return IdentityResult.Success;
        }

        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = request.User.Id,
            RoleId = role.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    private string RoleForTenant(string roleName, Guid? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId?.ToString() ?? requestContext.TenantId)
            ? roleName
            : $"{tenantId?.ToString() ?? requestContext.TenantId}:{roleName}";
}
