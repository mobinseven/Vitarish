using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Extensions;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class GetUsersHandler(UsersDbContext db, IDistributedCache cache) : IRequestHandler<GetUsers, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsers request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUsersAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        List<User> users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
        await UserCacheExtensions.SetUsersAsync(cache, users, cancellationToken);

        return users;
    }
}
