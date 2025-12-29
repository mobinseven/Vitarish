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
        return await dbContext.UserRoles.AsNoTracking()
            .Join(dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == needed, cancellationToken);
    }
}
