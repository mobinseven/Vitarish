using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class OnboardingTenantDirectory(PartiesDbContext dbContext) : IOnboardingTenantDirectory
{
    public Task<bool> IsOnboardingTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        dbContext.Tenants.AsNoTracking().AnyAsync(
            tenant => tenant.Id == tenantId && tenant.AppId == OnboardingTenant.AppId && tenant.DeletedAt == null,
            cancellationToken);
}
