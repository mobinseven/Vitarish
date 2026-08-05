using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class TenantExistence(PartiesDbContext dbContext) : ITenantExistence
{
    public Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.AsNoTracking().AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
}
