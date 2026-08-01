using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class EfAppTenantProvisioner(PartiesDbContext dbContext) : IAppTenantProvisioner
{
    public async Task EnsureAsync(EnsureAppTenantRequest request, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                await EnsureOnceAsync(request, cancellationToken);
                return;
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task EnsureOnceAsync(EnsureAppTenantRequest request, CancellationToken cancellationToken)
    {
        string hostname = HostnameNormalizer.Canonical(request.Hostname);
        TenantIndicator? claimedIndicator = await dbContext.TenantIndicators
            .AsNoTracking()
            .SingleOrDefaultAsync(indicator => indicator.Value == hostname, cancellationToken);
        if (claimedIndicator is not null && claimedIndicator.TenantId != request.TenantId)
            throw new InvalidOperationException("The configured app hostname belongs to another tenant.");

        Owner? owner = await dbContext.Owners
            .SingleOrDefaultAsync(item => item.Id == request.OwnerId, cancellationToken);
        if (owner is null)
        {
            owner = new Owner { Id = request.OwnerId, Name = request.Name };
            dbContext.Owners.Add(owner);
        }

        Tenant? tenant = await dbContext.Tenants
            .Include(item => item.Indicators)
            .SingleOrDefaultAsync(item => item.Id == request.TenantId, cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = request.TenantId,
                OwnerId = owner.Id,
                Name = request.Name,
                AppId = request.AppId
            };
            dbContext.Tenants.Add(tenant);
        }
        else if (!string.Equals(tenant.AppId, request.AppId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The reserved app tenant identifier belongs to another app.");
        }

        TenantIndicator? primary = tenant.Indicators.SingleOrDefault(indicator => indicator.IsPrimary);
        if (primary is null)
        {
            tenant.Indicators.Add(new TenantIndicator
            {
                Id = request.PrimaryIndicatorId,
                TenantId = tenant.Id,
                Method = IndicatorMethod.Hostname,
                Value = hostname,
                Status = IndicatorStatus.Active,
                IsPrimary = true
            });
        }
        else
        {
            primary.Method = IndicatorMethod.Hostname;
            primary.Value = hostname;
            primary.Status = IndicatorStatus.Active;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
