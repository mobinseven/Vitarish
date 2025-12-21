using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Clients.Identity.Contracts;
using Vitarish.Clients.Interface.Identity.Extensions;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Clients.Interface.Identity.Services;

internal class AccessAuthorizationHandler(
    IUserAccessApiClient userAccessClient,
    IRequestContextClient requestContextClient,
    IDistributedCache cache) : AuthorizationHandler<IAccessAuthorizationRequirement, string>
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

        // Build request context from current user and resource
        RequestDto? clientRequest = requestContextClient.Request;
        if (clientRequest is null)
        {
            // Fallback to legacy client-based check if request context is not available
            await HandleLegacyAuthorizationAsync(ctx, req, resource, userId);
            return;
        }

        // Convert to RequestIdentityDto
        RequestIdentityDto requestContext = ToRequestIdentityDto(clientRequest);

        // Use request-based authorization (new pattern)
        string key = $"{userId}:{resource}:{req.GetType().Name}:v2";

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
                        ? await IsAdminAsync(requestContext)
                        : await HasAccessAsync(requestContext);

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

    private async Task HandleLegacyAuthorizationAsync(
        AuthorizationHandlerContext ctx, IAccessAuthorizationRequirement req, string? resource, string userId)
    {
        string key = $"{userId}:{resource}:{req.GetType().Name}:v1";

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
                        ? await IsAdminLegacyAsync(userId, resource)
                        : await HasAccessLegacyAsync(userId, resource);

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

    // Request-based methods (new pattern)
    private async Task<bool> HasAccessAsync(RequestIdentityDto request) =>
        await userAccessClient.HasRequestAccess(request);

    private async Task<bool> IsAdminAsync(RequestIdentityDto request)
    {
        ClaimDto? claims = await userAccessClient.GetRequestAccessClaim(request);
        if (claims?.Value is null)
        {
            return false;
        }

        Access access = AccessExtensions.GenerateAccess(claims.Value);
        return access.Admin;
    }

    // Legacy client-based methods (deprecated)
    private async Task<bool> HasAccessLegacyAsync(string userId, string res) =>
        await userAccessClient.HasUserAccess(userId, res);

    private async Task<bool> IsAdminLegacyAsync(string userId, string res)
    {
        ClaimDto? claims = await userAccessClient.GetUserAccessClaim(userId, res);
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

    // Helper method to convert between DTO types
    private static RequestIdentityDto ToRequestIdentityDto(RequestDto dto) =>
        new(
            RequestId: dto.RequestId ?? Guid.NewGuid().ToString(),
            Authority: dto.Authority,
            ClientId: dto.ClientId,
            OwnerId: dto.OwnerId,
            TenantId: dto.TenantId,
            UserId: dto.UserId,
            ResourceId: dto.ResourceId,
            TraceId: null,
            AdditionalInfo: null
        );
}
