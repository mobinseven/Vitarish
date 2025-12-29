using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetUserRolesQueryHandler(UsersDbContext usersDb) : IRequestHandler<GetUserRolesQuery, IEnumerable<IdentityRole>>
{
    public async Task<IEnumerable<IdentityRole>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<IdentityRole> roles = await usersDb.UserRoles.Where(ur => ur.UserId == request.UserId)
                                                                    .Join(usersDb.Roles,
                                                                          ur => ur.RoleId,
                                                                          r => r.Id,
                                                                          (ur, r) => r)
                                                                    .AsNoTracking()
                                                                    .ToListAsync(cancellationToken: cancellationToken);
        return roles;
    }
}
