using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserByNameQueryHandler(
    UsersDbContext dbContext,
    IDistributedCache cache,
    ILookupNormalizer lookupNormalizer) : IRequestHandler<GetUserByNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNameQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawUserName))
        {
            throw new ArgumentException("User name is required.", nameof(request));
        }

        string? normalizedUserName = lookupNormalizer.NormalizeName(request.RawUserName);
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw new ArgumentException("User name must normalize to a non-empty value.", nameof(request));
        }

        if (await UserCacheExtensions.GetUserByNormalizedUserNameAsync(cache, normalizedUserName, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken: cancellationToken);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
