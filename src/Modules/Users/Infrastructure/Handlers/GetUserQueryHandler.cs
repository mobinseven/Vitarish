using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserQueryHandler(UsersDbContext dbContext, IDistributedCache cache) : IRequestHandler<GetUserQuery, User?>
{
    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
