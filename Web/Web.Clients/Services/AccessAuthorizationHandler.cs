using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Contracts;
using Vitarish.Web.Clients.Extensions;

namespace Vitarish.Web.Clients.Services;

internal class AccessAuthorizationHandler(
    IUserAccessApiClient apiClient,
    IDistributedCache cache,
    IRequestIdentityContext requestIdentityContext) : AuthorizationHandler<IAccessAuthorizationRequirement, string>
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, IAccessAuthorizationRequirement req, string? resource)
    {
        string? userId = ctx.User.GetId();
        if (userId is null)
        {
            ctx.Fail();
            return;
        }

        string ownerOrClient = resource ?? requestIdentityContext.OwnerId;
        string key = $"{userId}:{ownerOrClient}:{req.GetType().Name}";

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
                        ? await IsAdminAsync(userId, ownerOrClient, apiClient)
                        : await HasAccessAsync(userId, ownerOrClient, apiClient);

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

                if (gate.CurrentCount == 1 && gate.Wait(0))
                {
                    try
                    {
                        _ = _locks.TryRemove(key, out _);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }
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

    private static async Task<bool> HasAccessAsync(string userId, string res, IUserAccessApiClient api) =>
        await api.HasUserAccess(userId, res);

    private static async Task<bool> IsAdminAsync(string userId, string res, IUserAccessApiClient api)
    {
        ClaimDto? claims = await api.GetUserAccessClaim(userId, res);
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

    private static byte[] SerializeAccess(bool value) => [(byte)(value ? 1 : 0)];
}
