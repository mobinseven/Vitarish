namespace Vitarish.Parties.Tests;

using System.Security.Cryptography;
using System.Text;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Abstractions;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Handlers;
using Vitarish.Parties.Tests.TestHelpers;

/// <summary>
/// Tests for DNS verification functionality.
/// </summary>
public class DnsVerificationTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly NoOpPublisher _publisher;

    public DnsVerificationTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new NoOpPublisher();
        _dbContext = new PartiesDbContext(options, _publisher);
    }

    [Fact]
    public async Task AddTenantIndicator_WithDnsTxtMethod_GeneratesVerificationInfo()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new AddTenantIndicatorHandler(_dbContext, _publisher);
        var command = new AddTenantIndicator(
            tenant.Id,
            IndicatorMethod.Hostname,
            "example.com",
            false,
            IndicatorVerificationMethod.DnsTxt);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Verification);
        Assert.Equal(IndicatorVerificationMethod.DnsTxt, result.Verification.Method);
        Assert.Equal("_verify.example.com", result.Verification.Evidence);
        Assert.NotNull(result.Verification.Token);
        Assert.NotEmpty(result.Verification.Token);
        Assert.NotNull(result.Verification.EvidenceExpectedHash);
        Assert.NotEmpty(result.Verification.EvidenceExpectedHash);
        
        // Verify the hash matches the token
        byte[] tokenBytes = Encoding.UTF8.GetBytes($"verify={result.Verification.Token}");
        string expectedHash = Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();
        Assert.Equal(expectedHash, result.Verification.EvidenceExpectedHash);
    }

    [Fact]
    public async Task AddTenantIndicator_WithManualMethod_NoVerificationInfo()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client",
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var handler = new AddTenantIndicatorHandler(_dbContext, _publisher);
        var command = new AddTenantIndicator(
            tenant.Id,
            IndicatorMethod.Hostname,
            "example.com",
            false,
            IndicatorVerificationMethod.Manual);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Verification);
    }

    [Fact]
    public async Task VerifyTenantIndicator_WithManualMethod_AlwaysSucceeds()
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
            Value = "manual.example.com",
            Status = IndicatorStatus.PendingVerification,
            Verification = null // Manual method
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var dnsService = new MockDnsVerificationService(false); // Return false, but should still verify
        var handler = new VerifyTenantIndicatorHandler(_dbContext, _publisher, dnsService);
        var command = new VerifyTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Verified, updated.Status);
        Assert.NotNull(updated.VerifiedAt);
    }

    [Fact]
    public async Task VerifyTenantIndicator_WithDnsTxtMethod_SucceedsWhenDnsVerifies()
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
            Value = "dns.example.com",
            Status = IndicatorStatus.PendingVerification,
            Verification = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = "_verify.dns.example.com",
                EvidenceExpectedHash = "testhash",
                Token = "testtoken"
            }
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var dnsService = new MockDnsVerificationService(true);
        var handler = new VerifyTenantIndicatorHandler(_dbContext, _publisher, dnsService);
        var command = new VerifyTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.Verified, updated.Status);
        Assert.NotNull(updated.VerifiedAt);
    }

    [Fact]
    public async Task VerifyTenantIndicator_WithDnsTxtMethod_FailsWhenDnsVerificationFails()
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
            Value = "dns-fail.example.com",
            Status = IndicatorStatus.PendingVerification,
            Verification = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = "_verify.dns-fail.example.com",
                EvidenceExpectedHash = "testhash",
                Token = "testtoken"
            }
        };
        tenant.Indicators.Add(indicator);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var dnsService = new MockDnsVerificationService(false);
        var handler = new VerifyTenantIndicatorHandler(_dbContext, _publisher, dnsService);
        var command = new VerifyTenantIndicator(tenant.Id, indicator.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result); // Command succeeds but status doesn't change
        var updated = await _dbContext.TenantIndicators.FindAsync(indicator.Id);
        Assert.NotNull(updated);
        Assert.Equal(IndicatorStatus.PendingVerification, updated.Status); // Still pending
        Assert.Null(updated.VerifiedAt);
        Assert.NotNull(updated.LastCheckedAt); // But was checked
    }

    private class MockDnsVerificationService : IDnsVerificationService
    {
        private readonly bool _returnValue;

        public MockDnsVerificationService(bool returnValue = true)
        {
            _returnValue = returnValue;
        }

        public Task<bool> VerifyTxtRecordAsync(string hostname, string expectedHash, CancellationToken cancellationToken = default)
            => Task.FromResult(_returnValue);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
