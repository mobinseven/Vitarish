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
        return await (from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == needed
            select user).ToListAsync(cancellationToken);
    }
}
