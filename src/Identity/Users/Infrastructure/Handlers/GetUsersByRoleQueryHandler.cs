using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetUsersByRoleQueryHandler(UsersDbContext dbContext, IRequestContext requestContext) : IRequestHandler<GetUsersByRoleQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersByRoleQuery request, CancellationToken cancellationToken)
    {
        string needed = $"{requestContext.TenantId}:{request.RoleName}";
        string? roleId = await dbContext.Roles.AsNoTracking()
            .Where(role => role.Name == needed)
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (roleId is null)
        {
            return Array.Empty<User>();
        }

        return await dbContext.UserRoles.AsNoTracking()
            .Where(userRole => userRole.RoleId == roleId)
            .Join(dbContext.Users.AsNoTracking(),
                userRole => userRole.UserId,
                user => user.Id,
                (_, user) => user)
            .ToListAsync(cancellationToken);
    }
}
