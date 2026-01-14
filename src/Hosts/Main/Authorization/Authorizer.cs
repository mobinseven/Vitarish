using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Main.Infrastructure.Utilities;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Main.Authorization;

public sealed class Authorizer(IUsersService usersService, IDistributedCache cache, IRequestContext requestContext) : AuthorizationHandler<AuthorizerRequirement>
{
    private static readonly KeyedAsyncLock _keyedLock = new();
    private static readonly TimeSpan RoleCacheSuccessTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RoleCacheFailureTtl = TimeSpan.FromSeconds(15);

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizerRequirement requirement)
    {
        if (!context.User.IsAuthenticated())
        {
            context.Fail();
            return;
        }

        string? subjectId = context.User.GetSubjectId();

        if (string.IsNullOrWhiteSpace(subjectId))
        {
            context.Fail();
            return;
        }

        string requiredRole;
        bool result;
        switch (requirement.Role)
        {
            case IdentityAccessConstants.User:

                if (requirement.Role == IdentityAccessConstants.User)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityAccessConstants.Admin:

                requiredRole = $"{IdentityAccessConstants.Admin}";
                result = await ControlAndCache(context, requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityAccessConstants.Tenant:

                requiredRole = $"{requestContext.TenantId}:{IdentityAccessConstants.User}";
                result = await ControlAndCache(context, requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case IdentityAccessConstants.Owner:

                requiredRole = $"{requestContext.TenantId}:{IdentityAccessConstants.Owner}";
                result = await ControlAndCache(context, requiredRole, subjectId);
                if (result)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            default:
                break;
        }
        return;
    }

    private async Task<bool> ControlAndCache(AuthorizationHandlerContext context, string requirement, string subjectId)
    {
        if (context.User.IsInRole(requirement))
        {
            return true;
        }

        if (context.User.Claims.Any(c => c.Type == IdentityAccessConstants.AccessClaim && c.Value == requirement))
        {
            return true;
        }

        string cacheKey = BuildCacheKey(subjectId, requirement);
        bool? cached = await GetCachedBoolAsync(cache, cacheKey);
        if (cached.HasValue && cached.Value)
        {
            return true;
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
                hasRole = await usersService.CheckUserRoleAsync(subjectId, requirement);
                TimeSpan ttl = hasRole ? RoleCacheSuccessTtl : RoleCacheFailureTtl;
                await cache.SetAsync(
                    cacheKey,
                    SerializeBool(hasRole),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }

            if (hasRole)
            {
                return true;
            }
        }

        return false;
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
