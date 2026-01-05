using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class TenantsService(
    PartiesDbContext db,
    ILogger<TenantsService> logger) : ITenantsService
{
    public async Task<Tenant?> ResolveFromHostAsync(HttpContext httpContext, CancellationToken ct = default)
    {
        // Use ONLY trusted server-side values from HttpContext (after ForwardedHeaders middleware)
        string rawHost = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(rawHost))
        {
            logger.LogWarning("Tenant resolution failed: Host is empty");
            return null;
        }

        string canonical;
        try
        {
            canonical = HostnameNormalizer.Canonical(rawHost);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Tenant resolution failed: Invalid hostname format {Host}", rawHost);
            return null;
        }

        try
        {
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
                logger.LogInformation("No tenant found for {Host} (canonical: {Canonical})", rawHost, canonical);
                return null;
            }

            if (tenant.Count > 1)
            {
                // Multiple tenants match - this shouldn't happen with unique constraints
                logger.LogError("Multiple tenants found for {Host} (canonical: {Canonical})", rawHost, canonical);
                throw new InvalidOperationException($"Ambiguous tenant mapping for host: {canonical}");
            }

            Tenant resolved = tenant[0];
            logger.LogInformation(
                "Tenant resolved for {Host} (canonical: {Canonical}): TenantId={TenantId}, Name={Name}",
                rawHost, canonical, resolved.Id, resolved.Name);

            return resolved;

        }
        catch (OperationCanceledException ex)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
}
