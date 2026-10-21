using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class AppTenantDirectory(PartiesDbContext dbContext) : IAppTenantDirectory
{
    public Task<bool> IsLiveAppTenantAsync(Guid tenantId, string appId, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.AsNoTracking().AnyAsync(
            tenant => tenant.Id == tenantId && tenant.AppId == appId && tenant.DeletedAt == null,
            cancellationToken);
}
