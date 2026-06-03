using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserByNameQueryHandler(UsersDbContext dbContext, IDistributedCache cache) : IRequestHandler<GetUserByNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNameQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserByNameAsync(cache, request.UserName, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken: cancellationToken);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
