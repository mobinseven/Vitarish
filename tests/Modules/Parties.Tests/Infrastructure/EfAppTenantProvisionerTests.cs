using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Vitarish.Parties.Domain.Entities;
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

    [Fact]
    public async Task Ensure_SelectsEarliestOwnerWhenMultipleOwnersShareTheRequestOwnerName()
    {
        await using PartiesDbContext dbContext = CreateContext();
        Owner earlierOwner = new()
        {
            Id = Guid.NewGuid(),
            Name = "Vitarish",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        Owner laterSelfServiceOwner = new()
        {
            Id = Guid.NewGuid(),
            Name = "Vitarish",
            CreatedAt = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        dbContext.Owners.AddRange(laterSelfServiceOwner, earlierOwner);
        dbContext.Tenants.Add(new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = laterSelfServiceOwner.Id,
            Name = "Vitarish",
            AppId = "clinic-app",
            CreatedAt = laterSelfServiceOwner.CreatedAt
        });
        await dbContext.SaveChangesAsync();

        EfAppTenantProvisioner provisioner = new(dbContext);
        EnsureAppTenantRequest request = new(
            "Vitarish",
            "landing",
            "Landing",
            "landing.example.com");

        Guid tenantId = await provisioner.EnsureAsync(request);

        Tenant createdTenant = await dbContext.Tenants.SingleAsync(item => item.Id == tenantId);
        Assert.Equal(earlierOwner.Id, createdTenant.OwnerId);
    }

    [Fact]
    public async Task Ensure_UsesExistingAppTenantOwnerEvenWhenAnotherOwnerWithSameNameIsOlder()
    {
        await using PartiesDbContext dbContext = CreateContext();
        Owner olderOwner = new()
        {
            Id = Guid.NewGuid(),
            Name = "Vitarish",
            CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        Owner existingAppOwner = new()
        {
            Id = Guid.NewGuid(),
            Name = "Vitarish",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        Tenant existingAppTenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = existingAppOwner.Id,
            Name = "Landing",
            AppId = "landing"
        };
        dbContext.Owners.AddRange(olderOwner, existingAppOwner);
        dbContext.Tenants.Add(existingAppTenant);
        await dbContext.SaveChangesAsync();

        EfAppTenantProvisioner provisioner = new(dbContext);
        EnsureAppTenantRequest request = new(
            "Vitarish",
            "landing",
            "Landing",
            "landing.example.com");

        Guid tenantId = await provisioner.EnsureAsync(request);

        Assert.Equal(existingAppTenant.Id, tenantId);
        Tenant persistedTenant = await dbContext.Tenants.SingleAsync(item => item.Id == tenantId);
        Assert.Equal(existingAppOwner.Id, persistedTenant.OwnerId);
        Assert.Equal(2, await dbContext.Owners.CountAsync());
    }

    private static PartiesDbContext CreateContext()
    {
        DbContextOptions<PartiesDbContext> options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PartiesDbContext(options, Mock.Of<IPublisher>(), Array.Empty<IInterceptor>());
    }
}
