namespace Vitarish.Parties.Tests;

using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

public class DomainEntityTests
{
    [Fact]
    public void Tenant_InitializesIndicatorsCollection()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            ClientId = "client-id"
        };

        Assert.NotNull(tenant.Indicators);
        Assert.Empty(tenant.Indicators);
    }

    [Fact]
    public void Tenant_SetsCreatedAtOnConstruction()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            ClientId = "client-id"
        };

        Assert.True(tenant.CreatedAt >= before);
        Assert.True(tenant.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Owner_SetsCreatedAtOnConstruction()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };

        Assert.True(owner.CreatedAt >= before);
        Assert.True(owner.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void TenantIndicator_SetsDefaults()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Method = IndicatorMethod.Hostname,
            Value = "example.com"
        };

        Assert.Null(indicator.Verification);
        Assert.False(indicator.IsPrimary);
        Assert.True(indicator.CreatedAt >= before);
        Assert.True(indicator.CreatedAt <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void VerificationInfo_AllowsOptionalToken()
    {
        var info = new VerificationInfo
        {
            Method = IndicatorVerificationMethod.DnsTxt,
            Evidence = "_verify.example.com",
            EvidenceExpectedHash = "hash"
        };

        Assert.Null(info.Token);
        Assert.Equal(IndicatorVerificationMethod.DnsTxt, info.Method);
    }
}
