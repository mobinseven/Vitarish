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
        if (string.IsNullOrWhiteSpace(request.NormalizedUserName))
        {
            throw new ArgumentException("Normalized user name is required.", nameof(request));
        }

        if (await UserCacheExtensions.GetUserByNormalizedUserNameAsync(cache, request.NormalizedUserName, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == request.NormalizedUserName, cancellationToken);

        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
