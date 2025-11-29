
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Extensions;
using Vorn.Aaas.Client.Models;

namespace Vorn.Aaas.Client.Services;

internal class AccessAuthorizationHandler(
    IUserAccessApiClient apiClient,
    IMemoryCache cache,
    IRequestIdentityContext requestIdentityContext) : AuthorizationHandler<IAccessAuthorizationRequirement, string>
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, IAccessAuthorizationRequirement req, string? resource)
    {
        string? userId = ctx.User.GetId();
        if (userId is null)
        { ctx.Fail(); return; }

        string ownerOrClient = resource ?? requestIdentityContext.OwnerId;
        string key = $"{userId}:{ownerOrClient}:{req.GetType().Name}";

        if (!cache.TryGetValue(key, out bool hasAccess))
        {
            SemaphoreSlim gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (!cache.TryGetValue(key, out hasAccess))
                {
                    hasAccess = req is AdminAccessClaimRequirement
                        ? await IsAdminAsync(userId, ownerOrClient, apiClient)
                        : await HasAccessAsync(userId, ownerOrClient, apiClient);

                    TimeSpan ttl = hasAccess ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(45);
                    _ = cache.Set(key, hasAccess,
                        new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
                }
            }
            finally
            {
                _ = gate.Release();
                _ = _locks.TryRemove(key, out _);
            }
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

}
