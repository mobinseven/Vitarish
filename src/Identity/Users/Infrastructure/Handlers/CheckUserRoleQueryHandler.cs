using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class CheckUserRoleQueryHandler(UsersDbContext dbContext) : IRequestHandler<CheckUserRoleQuery, bool>
{
    public async Task<bool> Handle(CheckUserRoleQuery request, CancellationToken cancellationToken) => await dbContext.UserRoles.AsNoTracking()
            .Join(dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .AnyAsync(x => x.UserId == request.UserId && x.Name == request.RoleName, cancellationToken);
}
