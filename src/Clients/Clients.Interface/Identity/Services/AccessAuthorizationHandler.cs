using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Clients.Interface.Identity.Extensions;

namespace Vitarish.Clients.Interface.Identity.Services;

internal class AccessAuthorizationHandler(
    IUserAccessApiClient apiClient,
    IDistributedCache cache) : AuthorizationHandler<IAccessAuthorizationRequirement, string>
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, IAccessAuthorizationRequirement req, string? resource)
    {
        string? userId = ctx.User.GetId();
        if (userId is null)
        { ctx.Fail(); return; }
        string key = $"{userId}:{resource}:{req.GetType().Name}";

        bool? cachedAccess = await GetCachedAccessAsync(key);
        bool hasAccess;

        if (cachedAccess is null)
        {
            SemaphoreSlim gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                cachedAccess = await GetCachedAccessAsync(key);
                if (cachedAccess is null)
                {
                    hasAccess = req is AdminAccessClaimRequirement
                        ? await IsAdminAsync(userId, resource)
                        : await HasAccessAsync(userId, resource);

                    TimeSpan ttl = hasAccess ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(45);
                    await cache.SetAsync(
                        key,
                        SerializeAccess(hasAccess),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
                }
                else
                {
                    hasAccess = cachedAccess.Value;
                }
            }
            finally
            {
                _ = gate.Release();
                _ = _locks.TryRemove(key, out _);
            }
        }
        else
        {
            hasAccess = cachedAccess.Value;
        }

        if (hasAccess)
        {
            ctx.Succeed(req);
        }
        else
        {
            ctx.Fail();
        }
    }

    private async Task<bool> HasAccessAsync(string userId, string res) =>
        await apiClient.HasUserAccess(userId, res);

    private async Task<bool> IsAdminAsync(string userId, string res)
    {
        ClaimDto? claims = await apiClient.GetUserAccessClaim(userId, res);
        if (claims?.Value is null)
        {
            return false;
        }

        Access access = AccessExtensions.GenerateAccess(claims.Value);
        return access.Admin;
    }

    private async Task<bool?> GetCachedAccessAsync(string key)
    {
        byte[]? payload = await cache.GetAsync(key);
        return payload is null || payload.Length == 0 ? null : payload[0] is 1;
    }

    private static byte[] SerializeAccess(bool value) => new[] { value ? (byte)1 : (byte)0 };

}
