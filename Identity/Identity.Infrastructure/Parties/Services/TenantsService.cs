using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Identity.Core.Parties.Application.Services;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Enums;
using Vitarish.Identity.Infrastructure.Parties.Data;
using Vitarish.Identity.Infrastructure.Parties.Extensions;

namespace Vitarish.Identity.Infrastructure.Parties.Services;

internal sealed class TenantsService(PartiesDbContext db, ILogger<TenantsService> logger) : ITenantsService
{
    public async Task<Tenant?> ResolveFromHostAsync(string host, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("Tenant resolution failed: Host is empty");
            return null;
        }

        string canonical;
        try
        {
            canonical = HostnameNormalizer.Canonical(host);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Tenant resolution failed: Invalid hostname format {Host}", host);
            return null;
        }

        List<Tenant> tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Indicators)
            .Where(t => t.Indicators.Any(i =>
                i.Status == IndicatorStatus.Active &&
                (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
                i.Value == canonical))
            .ToListAsync(ct);

        if (tenant.Count == 0)
        {
            logger.LogInformation("No tenant found for {Host} (canonical: {Canonical})", host, canonical);
            return null;
        }

        if (tenant.Count > 1)
        {
            logger.LogError("Multiple tenants found for {Host} (canonical: {Canonical})", host, canonical);
            throw new InvalidOperationException($"Ambiguous tenant mapping for host: {canonical}");
        }

        Tenant resolved = tenant[0];
        logger.LogInformation(
            "Tenant resolved for {Host} (canonical: {Canonical}): TenantId={TenantId}, Name={Name}",
            host, canonical, resolved.Id, resolved.Name);

        return resolved;
    }
}
