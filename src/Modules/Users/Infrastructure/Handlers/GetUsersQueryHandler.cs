using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUsersQueryHandler(UsersDbContext db, IDistributedCache cache) : IRequestHandler<GetUsersQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUsersAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        User[] users = await db.Users.AsNoTracking().ToArrayAsync(cancellationToken);
        await UserCacheExtensions.SetUsersAsync(cache, users, cancellationToken);
        return users;
    }
}
