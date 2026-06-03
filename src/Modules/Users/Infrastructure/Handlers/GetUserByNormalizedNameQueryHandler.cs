using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class GetUserByNormalizedNameQueryHandler(
    UsersDbContext dbContext,
    IDistributedCache cache) : IRequestHandler<GetUserByNormalizedNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNormalizedNameQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == request.NormalizedUserName, cancellationToken);

        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
