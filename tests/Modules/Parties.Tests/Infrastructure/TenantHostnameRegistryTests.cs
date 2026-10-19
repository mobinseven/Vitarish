using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class TenantHostnameRegistryTests
{
    [Theory]
    [InlineData(IndicatorMethod.Subdomain, IndicatorStatus.Active)]
    [InlineData(IndicatorMethod.Hostname, IndicatorStatus.Disabled)]
    [InlineData(IndicatorMethod.Subdomain, IndicatorStatus.PendingVerification)]
    public async Task Any_indicator_on_the_hostname_claims_it_whatever_its_method_or_status(IndicatorMethod method, IndicatorStatus status)
    {
        await using PartiesDbContext db = CreateContext();
        Tenant tenant = new() { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Tenant", AppId = "app" };
        db.Tenants.Add(tenant);
        db.TenantIndicators.Add(new TenantIndicator { Id = Guid.NewGuid(), TenantId = tenant.Id, Method = method, Value = "rezaei.clinic.example.test", Status = status });
        await db.SaveChangesAsync();
        TenantHostnameRegistry registry = new(db);

        Assert.True(await registry.IsClaimedAsync("Rezaei.Clinic.Example.Test."));
        Assert.False(await registry.IsClaimedAsync("other.clinic.example.test"));
    }

    private static PartiesDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
        Mock.Of<MediatR.IPublisher>(), Array.Empty<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
}
