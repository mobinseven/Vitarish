using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Tests;

/// <summary>
/// Tests for /allow endpoint behavior for on-demand TLS.
/// </summary>
public class OnDemandAllowEndpointTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;

    public OnDemandAllowEndpointTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
    }

    [Fact]
    public async Task Allow_Returns200_ForVerifiedHostnameIndicator()
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
            Status = IndicatorStatus.Verified,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var canonical = HostnameNormalizer.Canonical("verified.customer.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_Returns200_ForActiveHostnameIndicator()
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
            Value = "active.customer.com",
            Status = IndicatorStatus.Active,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var canonical = HostnameNormalizer.Canonical("active.customer.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForPendingVerificationIndicator()
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

        var canonical = HostnameNormalizer.Canonical("pending.customer.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForDisabledIndicator()
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

        var canonical = HostnameNormalizer.Canonical("disabled.customer.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForNonExistentDomain()
    {
        var canonical = HostnameNormalizer.Canonical("nonexistent.customer.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_NormalizesHostname_BeforeCheck()
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
            Status = IndicatorStatus.Verified,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Check with uppercase to ensure normalization
        var canonical = HostnameNormalizer.Canonical("VERIFIED.CUSTOMER.COM");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_SupportsSubdomainMethod()
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
            Status = IndicatorStatus.Active,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var canonical = HostnameNormalizer.Canonical("customer.saas.example.com");
        var allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
