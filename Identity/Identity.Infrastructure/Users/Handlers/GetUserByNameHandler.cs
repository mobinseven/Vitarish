using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Extensions;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class GetUserByNameHandler(UsersDbContext db, IDistributedCache cache) : IRequestHandler<GetUserByName, User?>
{
    public async Task<User?> Handle(GetUserByName request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserByNameAsync(cache, request.UserName, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
