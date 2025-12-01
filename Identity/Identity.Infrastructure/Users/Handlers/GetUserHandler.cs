using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Extensions;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class GetUserHandler(UsersDbContext db, IDistributedCache cache) : IRequestHandler<GetUser, User?>
{
    public async Task<User?> Handle(GetUser request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
