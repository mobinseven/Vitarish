using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Main.Infrastructure.Utilities;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Main.Authorization;

public sealed class Authorizer(UserManager<User> userManager, IDistributedCache cache) : AuthorizationHandler<AuthorizerRequirement>
{
    private static readonly KeyedAsyncLock _keyedLock = new();
    private static readonly TimeSpan RoleCacheSuccessTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RoleCacheFailureTtl = TimeSpan.FromSeconds(45);

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizerRequirement requirement)
    {
        if (context.User.IsInRole(requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }
        if (context.User.Claims.Any(c => c.Type == IdentityAccessConstants.AccessClaim && c.Value == requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }

        string? subjectId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return;
        }

        string cacheKey = BuildCacheKey(subjectId, requirement.Role);
        bool? cached = await GetCachedBoolAsync(cache, cacheKey);
        if (cached.HasValue)
        {
            if (cached.Value)
            {
                context.Succeed(requirement);
            }

            return;
        }

        await using (await _keyedLock.LockAsync(cacheKey))
        {
            cached = await GetCachedBoolAsync(cache, cacheKey);
            bool hasRole;

            if (cached.HasValue)
            {
                hasRole = cached.Value;
            }
            else
            {
                var user = await userManager.FindByIdAsync(subjectId);
                hasRole = user is not null && await userManager.IsInRoleAsync(user, requirement.Role);

                TimeSpan ttl = hasRole ? RoleCacheSuccessTtl : RoleCacheFailureTtl;
                await cache.SetAsync(
                    cacheKey,
                    SerializeBool(hasRole),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }

            if (hasRole)
            {
                context.Succeed(requirement);
            }
        }
    }

    private static string BuildCacheKey(string userId, string role) =>
        $"Authorizer:{EscapeSegment(userId)}:{EscapeSegment(role)}";

    private static string EscapeSegment(string value) => Uri.EscapeDataString(value);

    private static async Task<bool?> GetCachedBoolAsync(IDistributedCache distributedCache, string key)
    {
        byte[]? payload = await distributedCache.GetAsync(key);
        return payload is null || payload.Length == 0 ? null : payload[0] == 1;
    }

    private static byte[] SerializeBool(bool value) => [value ? (byte)1 : (byte)0];
}
