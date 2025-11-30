using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Enums;
using Vitarish.Identity.Infrastructure.Data;
using Vitarish.Identity.Infrastructure.Services;
using Vitarish.Identity.Infrastructure.Tests.TestHelpers;

namespace Vitarish.Identity.Infrastructure.Tests.Services;

/// <summary>
/// Tests for TenantsService functionality.
/// </summary>
public class TenantsServiceTests : IDisposable
{
    private readonly IdentityDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly TenantsService _service;

    public TenantsServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new IdentityDbContext(options, new NoOpPublisher());
        _cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLoggerFactory.Instance.CreateLogger<TenantsService>();
        _service = new TenantsService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsNull_WhenHostIsEmpty()
    {
        var result = await _service.ResolveFromHostAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsNull_WhenHostIsWhitespace()
    {
        var result = await _service.ResolveFromHostAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsTenant_ForActiveHostnameIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.Active
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("app.customer.com");

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsNull_ForPendingVerificationIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "pending.customer.com",
            Status = IndicatorStatus.PendingVerification
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("pending.customer.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsNull_ForVerifiedButNotActiveIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "verified.customer.com",
            Status = IndicatorStatus.Verified
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("verified.customer.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_ReturnsNull_ForDisabledIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "disabled.customer.com",
            Status = IndicatorStatus.Disabled
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("disabled.customer.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFromHostAsync_NormalizesHostname_BeforeResolution()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.Active
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("APP.CUSTOMER.COM");

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveFromHostAsync_UsesCache_OnSecondRequest()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.Active
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // First call to populate cache
        var result1 = await _service.ResolveFromHostAsync("app.customer.com");

        // Remove tenant from DB to ensure cache is used
        _dbContext.Tenants.Remove(tenant);
        await _dbContext.SaveChangesAsync();

        // Second call should use cached value
        var result2 = await _service.ResolveFromHostAsync("app.customer.com");

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Id, result2.Id);
    }

    [Fact]
    public async Task ResolveFromHostAsync_SupportsSubdomainMethod()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        tenant.Indicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "customer.saas.example.com",
            Status = IndicatorStatus.Active
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = await _service.ResolveFromHostAsync("customer.saas.example.com");

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _cache?.Dispose();
    }
}
