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
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "landing", "Landing", "LANDING.Example.com.");

        await provisioner.EnsureAsync(request);
        await provisioner.EnsureAsync(request);

        Assert.Single(dbContext.Owners);
        Assert.Single(dbContext.Tenants);
        Assert.Single(dbContext.TenantIndicators);
        Assert.Equal("landing.example.com", dbContext.TenantIndicators.Single().Value);
        Assert.Equal(request.TenantId, dbContext.TenantIndicators.Single().TenantId);
    }

    [Fact]
    public async Task Ensure_RejectsHostnameClaimedByAnotherTenant()
    {
        await using PartiesDbContext dbContext = CreateContext();
        EfAppTenantProvisioner provisioner = new(dbContext);
        EnsureAppTenantRequest first = new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "first", "First", "landing.example.com");
        await provisioner.EnsureAsync(first);
        EnsureAppTenantRequest second = new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
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
