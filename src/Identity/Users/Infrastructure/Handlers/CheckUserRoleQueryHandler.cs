using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class CheckUserRoleQueryHandler(UsersDbContext dbContext, IRequestContext requestContext) : IRequestHandler<CheckUserRoleQuery, bool>
{
    public async Task<bool> Handle(CheckUserRoleQuery request, CancellationToken cancellationToken)
    {
        string needed = $"{requestContext.TenantId}:{request.RoleName}";
        string userId = requestContext.UserId ?? throw new InvalidDataException("IRequestContext provided a null user ID");
        string? roleId = await dbContext.Roles.AsNoTracking()
            .Where(role => role.Name == needed)
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (roleId is null)
        {
            return false;
        }

        return await dbContext.UserRoles.AsNoTracking()
            .AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId, cancellationToken);
    }
}
