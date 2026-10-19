using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class TenantHostnameRegistry(PartiesDbContext dbContext) : ITenantHostnameRegistry
{
    public Task<bool> IsClaimedAsync(string hostname, CancellationToken cancellationToken = default)
    {
        string normalized = HostnameNormalizer.Canonical(hostname);
        return dbContext.TenantIndicators.AsNoTracking().AnyAsync(indicator => indicator.Value == normalized, cancellationToken);
    }
}
