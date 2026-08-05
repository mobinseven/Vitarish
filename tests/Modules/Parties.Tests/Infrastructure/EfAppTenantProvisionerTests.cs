using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class EfAppTenantProvisionerTests
{
    [Fact]
    public async Task Ensure_CreatesCanonicalTenantAndIsIdempotent()
    {
        await using PartiesDbContext dbContext = CreateContext();
        EfAppTenantProvisioner provisioner = new(dbContext);
        EnsureAppTenantRequest request = new(
            "Vitarish",
            "landing", "Landing", "LANDING.Example.com.");

        Guid tenantId = await provisioner.EnsureAsync(request);
        Assert.Equal(tenantId, (await provisioner.EnsureAsync(request)));

        Assert.Single(dbContext.Owners);
        Assert.Single(dbContext.Tenants);
        Assert.Single(dbContext.TenantIndicators);
        Assert.Equal("landing.example.com", dbContext.TenantIndicators.Single().Value);
        Assert.Equal(tenantId, dbContext.TenantIndicators.Single().TenantId);
    }

    [Fact]
    public async Task Ensure_RejectsHostnameClaimedByAnotherTenant()
    {
        await using PartiesDbContext dbContext = CreateContext();
        EfAppTenantProvisioner provisioner = new(dbContext);
        EnsureAppTenantRequest first = new(
            "First",
            "first", "First", "landing.example.com");
        await provisioner.EnsureAsync(first);
        EnsureAppTenantRequest second = new(
            "Second",
            "second", "Second", "landing.example.com");

        Func<Task> action = () => provisioner.EnsureAsync(second);

        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    private static PartiesDbContext CreateContext()
    {
        DbContextOptions<PartiesDbContext> options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PartiesDbContext(options, Mock.Of<IPublisher>(), Array.Empty<IInterceptor>());
    }
}
