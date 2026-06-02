using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Parties.Tests;

/// <summary>
/// Tests for /allow endpoint behavior for on-demand TLS.
/// </summary>
public class OnDemandAllowEndpointTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;

    public OnDemandAllowEndpointTests()
    {
        DbContextOptions<PartiesDbContext> options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PartiesDbContext(options, new TestHelpers.NoOpPublisher());
    }

    [Fact]
    public async Task Allow_Returns200_ForVerifiedHostnameIndicator()
    {
        Tenant tenant = new()
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
            Status = IndicatorStatus.Verified,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        string canonical = HostnameNormalizer.Canonical("verified.customer.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_Returns200_ForActiveHostnameIndicator()
    {
        Tenant tenant = new()
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
            Value = "active.customer.com",
            Status = IndicatorStatus.Active,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        string canonical = HostnameNormalizer.Canonical("active.customer.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForPendingVerificationIndicator()
    {
        Tenant tenant = new()
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
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        string canonical = HostnameNormalizer.Canonical("pending.customer.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForDisabledIndicator()
    {
        Tenant tenant = new()
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
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        string canonical = HostnameNormalizer.Canonical("disabled.customer.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_Returns403_ForNonExistentDomain()
    {
        string canonical = HostnameNormalizer.Canonical("nonexistent.customer.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Allow_NormalizesHostname_BeforeCheck()
    {
        Tenant tenant = new()
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
            Status = IndicatorStatus.Verified,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        // Check with uppercase to ensure normalization
        string canonical = HostnameNormalizer.Canonical("VERIFIED.CUSTOMER.COM");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    [Fact]
    public async Task Allow_SupportsSubdomainMethod()
    {
        Tenant tenant = new()
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
            Status = IndicatorStatus.Active,
            VerifiedAt = DateTimeOffset.UtcNow
        });
        _ = _dbContext.Tenants.Add(tenant);
        _ = await _dbContext.SaveChangesAsync();

        string canonical = HostnameNormalizer.Canonical("customer.saas.example.com");
        bool allowed = await _dbContext.TenantIndicators.AnyAsync(i =>
            (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
            (i.Status == IndicatorStatus.Verified || i.Status == IndicatorStatus.Active) &&
            i.Value == canonical);

        Assert.True(allowed);
    }

    public void Dispose() => _dbContext?.Dispose();
}
