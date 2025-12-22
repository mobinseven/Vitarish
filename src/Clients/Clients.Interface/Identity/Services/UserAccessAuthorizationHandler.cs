using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Clients.Interface.Identity.Extensions;

namespace Vitarish.Clients.Interface.Identity.Services
{
    internal class UserAccessAuthorizationHandler(
        IUserAccessClient userAccessClient,
        IDistributedCache cache
    ) : AuthorizationHandler<UserAccessRequirement, string>
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext ctx,
            UserAccessRequirement requirement,
            string resource)
        {
            if (resource is null)
            {
                return;
            }

            string? userId = ctx.User.GetId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            string key = UserAccessAuthorizationHelpers.BuildAuthenticationCacheKey(userId, resource);

            bool hasAccess;

            bool? cachedAccess = await GetCachedAccessAsync(key);
            if (cachedAccess.HasValue)
            {
                hasAccess = cachedAccess.Value;
            }
            else
            {
                SemaphoreSlim gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync();
                try
                {
                    cachedAccess = await GetCachedAccessAsync(key);
                    if (cachedAccess.HasValue)
                    {
                        hasAccess = cachedAccess.Value;
                    }
                    else
                    {
                        hasAccess = await userAccessClient.CheckUserAccess(resource);

                        TimeSpan ttl = hasAccess ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(45);
                        await cache.SetAsync(
                            key,
                            SerializeAccess(hasAccess),
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
                    }
                }
                finally
                {
                    _ = gate.Release();
                }
            }

            if (hasAccess)
            {
                ctx.Succeed(requirement);
            }
        }

        private async Task<bool?> GetCachedAccessAsync(string key)
        {
            byte[]? payload = await cache.GetAsync(key);
            return payload is null || payload.Length == 0 ? null : payload[0] == 1;
        }

        private static byte[] SerializeAccess(bool value) => [value ? (byte)1 : (byte)0];
    }
}
