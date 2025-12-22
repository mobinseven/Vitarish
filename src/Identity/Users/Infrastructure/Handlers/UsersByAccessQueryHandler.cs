using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class UsersByAccessQueryHandler(UsersDbContext usersDbContext, IRequestContext requestContext) : IRequestHandler<UsersByAccessQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(UsersByAccessQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> query =
            from ua in usersDbContext.UsersAccess.AsNoTracking()
            where ua.ClientId == requestContext.ClientId
               && ua.OwnerId == requestContext.OwnerId
               && ua.TenantId == requestContext.TenantId
               && ua.ResourceId == requestContext.ResourceId
               && ua.UserId != null
            join u in usersDbContext.Users.AsNoTracking()
              on ua.UserId equals u.Id
            select u;

        return await query.Distinct().ToListAsync(cancellationToken);
    }
}
