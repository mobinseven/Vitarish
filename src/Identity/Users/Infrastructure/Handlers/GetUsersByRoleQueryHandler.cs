using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetUsersByRoleQueryHandler(UsersDbContext dbContext) : IRequestHandler<GetUsersByRoleQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersByRoleQuery request, CancellationToken cancellationToken) => await (from user in dbContext.Users.AsNoTracking()
                                                                                                                            join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                                                                                                                            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                                                                                                            where role.Name == request.RoleName
                                                                                                                            select user).ToListAsync(cancellationToken);
}
