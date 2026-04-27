namespace Parties.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Parties.Domain.Enums;

/// <summary>
/// Tests for tenant resolution functionality.
/// </summary>
public class TenantResolverTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly TenantResolver _resolver;

    public TenantResolverTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
        _cache = new MemoryCache(new MemoryCacheOptions());
        _resolver = new TenantResolver(_dbContext, _cache);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenHostIsEmpty()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsTenant_ForActiveHostnameIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("app.customer.com");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_ForPendingVerificationIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("pending.customer.com");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_ForVerifiedButNotActiveIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("verified.customer.com");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_ForDisabledIndicator()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("disabled.customer.com");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NormalizesHostname_BeforeResolution()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("APP.CUSTOMER.COM");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task ResolveAsync_UsesCache_OnSecondRequest()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext1 = new DefaultHttpContext();
        httpContext1.Request.Host = new HostString("app.customer.com");
        Tenant? result1 = await _resolver.ResolveAsync(httpContext1);

        // Remove tenant from DB to ensure cache is used
        _dbContext.Tenants.Remove(tenant);
        await _dbContext.SaveChangesAsync();

        var httpContext2 = new DefaultHttpContext();
        httpContext2.Request.Host = new HostString("app.customer.com");
        Tenant? result2 = await _resolver.ResolveAsync(httpContext2);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Id, result2.Id);
    }

    [Fact]
    public async Task ResolveAsync_SupportsSubdomainMethod()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            AppId = "test-client",
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("customer.saas.example.com");

        Tenant? result = await _resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _cache?.Dispose();
    }
}
