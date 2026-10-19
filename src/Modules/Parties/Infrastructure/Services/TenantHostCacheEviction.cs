using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Vitarish.Parties.Domain.Events;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>Drops this instance's cached tenant and origin-trust answers for changed hostnames.</summary>
public sealed class TenantHostCacheEviction(IMemoryCache cache) : INotificationHandler<TenantHostnamesChangedEvent>
{
    public Task Handle(TenantHostnamesChangedEvent notification, CancellationToken cancellationToken)
    {
        foreach (string hostname in notification.Hostnames)
        {
            cache.Remove(TenantHostCacheKeys.Resolver(hostname));
            cache.Remove(TenantHostCacheKeys.OriginTrust(hostname));
        }

        return Task.CompletedTask;
    }
}
