namespace Vorn.Aaas.Parties.Tests;

using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Abstractions;
using Vorn.Aaas.Parties.Tests.TestHelpers;

/// <summary>
/// Tests for tenant indicator status transition logic.
/// </summary>
public class IndicatorStatusTransitionTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly NoOpPublisher _publisher;
    private readonly MockDnsVerificationService _dnsService;

    public IndicatorStatusTransitionTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new NoOpPublisher();
        _dbContext = new PartiesDbContext(options, _publisher);
        _dnsService = new MockDnsVerificationService();
    }

    private class MockDnsVerificationService : IDnsVerificationService
    {
        public Task<bool> VerifyTxtRecordAsync(string hostname, string expectedHash, CancellationToken cancellationToken = default)
            => Task.FromResult(true); // Always return true for tests
    }

    [Fact]
    public async Task VerifyTenantIndicator_TransitionsPendingToVerified()
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
            Value = "test.example.com",
            Status = IndicatorStatus.PendingVerification,
            Verification = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = "_verify.test.example.com",
                EvidenceExpectedHash = "hash"
            }
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new VerifyTenantIndicatorHandler(_dbContext, _publisher, _dnsService);
        var command = new VerifyTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Verified, updated.Status);
        Assert.NotNull(updated.VerifiedAt);
        Assert.NotNull(updated.LastCheckedAt);
    }

    [Fact]
    public async Task VerifyTenantIndicator_DoesNotDowngradeActiveStatus()
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
            Value = "active.example.com",
            Status = IndicatorStatus.Active,
            VerifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new VerifyTenantIndicatorHandler(_dbContext, _publisher, _dnsService);
        var command = new VerifyTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Active, updated.Status);
        Assert.NotNull(updated.LastCheckedAt);
    }

    [Fact]
    public async Task ActivateTenantIndicator_TransitionsVerifiedToActive()
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
            Value = "verified.example.com",
            Status = IndicatorStatus.Verified,
            VerifiedAt = DateTimeOffset.UtcNow
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new ActivateTenantIndicatorHandler(_dbContext, _publisher);
        var command = new ActivateTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Active, updated.Status);
    }

    [Fact]
    public async Task ActivateTenantIndicator_FailsForPendingVerification()
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
            Value = "pending.example.com",
            Status = IndicatorStatus.PendingVerification
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new ActivateTenantIndicatorHandler(_dbContext, _publisher);
        var command = new ActivateTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.PendingVerification, updated.Status);
    }

    [Fact]
    public async Task DisableTenantIndicator_TransitionsToDisabled()
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
            Value = "active.example.com",
            Status = IndicatorStatus.Active
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new DisableTenantIndicatorHandler(_dbContext, _publisher);
        var command = new DisableTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Disabled, updated.Status);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
