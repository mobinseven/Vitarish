using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Enums;
using Vitarish.Identity.Infrastructure.Data;
using Vitarish.Identity.Infrastructure.Tests.TestHelpers;

namespace Vitarish.Identity.Infrastructure.Tests.Data;

/// <summary>
/// Tests for IdentityDbContext functionality.
/// </summary>
public class IdentityDbContextTests : IDisposable
{
    private readonly IdentityDbContext _dbContext;

    public IdentityDbContextTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new IdentityDbContext(options, new NoOpPublisher());
    }

    [Fact]
    public async Task SaveChangesAsync_NormalizesHostname_OnAdd()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "APP.CUSTOMER.COM",
            Status = IndicatorStatus.Active
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);

        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(saved);
        Assert.Equal("app.customer.com", saved.Value);
    }

    [Fact]
    public async Task SaveChangesAsync_NormalizesHostname_OnModify()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.Active
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Modify the value
        indicator.Value = "NEW.CUSTOMER.COM";
        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(saved);
        Assert.Equal("new.customer.com", saved.Value);
    }

    [Fact]
    public async Task SaveChangesAsync_CanSaveOwner()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Test Owner",
            LogoUrl = "https://example.com/logo.png",
            Description = "A test owner"
        };

        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.Owners.FindAsync(owner.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Owner", saved.Name);
    }

    [Fact]
    public async Task SaveChangesAsync_CanSaveTenantWithIndicators()
    {
        var ownerId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Test Tenant",
            ClientId = "test-client"
        };
        var indicator1 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        };
        var indicator2 = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Subdomain,
            Value = "customer.saas.example.com",
            Status = IndicatorStatus.Active
        };
        tenant.Indicators.Add(indicator1);
        tenant.Indicators.Add(indicator2);

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.Tenants
            .Include(t => t.Indicators)
            .FirstOrDefaultAsync(t => t.Id == tenant.Id);
        
        Assert.NotNull(saved);
        Assert.Equal(2, saved.Indicators.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_CanSaveAuthenticationClient()
    {
        var client = new AuthenticationClient
        {
            Id = "test-client-id",
            Url = "https://example.com",
            Title = "Test Client",
            Description = "A test client",
            AutoAccess = true
        };

        _dbContext.AuthenticationClients.Add(client);
        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.AuthenticationClients.FindAsync(client.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test Client", saved.Title);
    }

    [Fact]
    public async Task SaveChangesAsync_CanSaveVerificationInfo()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client"
        };
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = IndicatorMethod.Hostname,
            Value = "app.customer.com",
            Status = IndicatorStatus.PendingVerification,
            Verification = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = "_verify.app.customer.com",
                EvidenceExpectedHash = "abc123hash",
                Token = "verify=secret-token"
            }
        };
        tenant.Indicators.Add(indicator);

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var saved = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(saved);
        Assert.NotNull(saved.Verification);
        Assert.Equal(IndicatorVerificationMethod.DnsTxt, saved.Verification.Method);
        Assert.Equal("_verify.app.customer.com", saved.Verification.Evidence);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
