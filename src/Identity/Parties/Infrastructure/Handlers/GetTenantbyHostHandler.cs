using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Handlers;

internal class GetTenantbyHostHandler(PartiesDbContext db) : IRequestHandler<GetTenantByHost, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenantByHost request, CancellationToken cancellationToken)
    {

        string canonical;
        try
        {
            canonical = HostnameNormalizer.Canonical(request.Host);
        }
        catch (ArgumentException ex)
        {
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
                .ToListAsync(cancellationToken);

            if (tenant.Count == 0)
            {
                return null;
            }

            if (tenant.Count > 1)
            {
                // Multiple tenants match - this shouldn't happen with unique constraints
                throw new InvalidOperationException($"Ambiguous tenant mapping for host: {canonical}");
            }

            Tenant resolved = tenant[0];
            return resolved;

        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
}
