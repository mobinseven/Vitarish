using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Vitarish.Clients.Interface.Identity.Services;

public sealed class TenantRoleHandler(
    IIdentityService identityService,
    IUsersService usersService,
    IHttpContextAccessor http,
    IDistributedCache cache
) : AuthorizationHandler<TenantRoleRequirement>
{
    private static readonly KeyedAsyncLock _keyedLock = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRoleRequirement requirement)
    {
        if (context.User.IsInRole(requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }

        string? userId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        identityService.User ??= context.User;

        string key = BuildKey(userId, http.HttpContext?.Request.Host.Value, requirement.Role);

        // Fast path
        bool? cached = await GetCachedBoolAsync(cache, key);
        if (cached.HasValue)
        {
            if (cached.Value)
            {
                context.Succeed(requirement);
            }

            return;
        }

        // Cache miss: single-flight per key (per process)
        await using (await _keyedLock.LockAsync(key))
        {
            cached = await GetCachedBoolAsync(cache, key);
            bool hasRole;

            if (cached.HasValue)
            {
                hasRole = cached.Value;
            }
            else
            {
                // If CheckUserRole actually needs tenant/user inputs, pass them here instead of only Role.
                hasRole = await usersService.CheckUserRole(requirement.Role);

                TimeSpan ttl = hasRole ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(45); // tune as needed
                await cache.SetAsync(
                    key,
                    SerializeBool(hasRole),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }

            if (hasRole)
            {
                context.Succeed(requirement);
            }
        }
    }

    private static string BuildKey(string userId, string? tenantId, string role) =>
        $"TenantRole:{tenantId}:{userId}:{role}";

    private static async Task<bool?> GetCachedBoolAsync(IDistributedCache cache, string key)
    {
        byte[]? payload = await cache.GetAsync(key);
        return payload is null || payload.Length == 0 ? null : payload[0] == 1;
    }

    private static byte[] SerializeBool(bool value) => [value ? (byte)1 : (byte)0];
}
