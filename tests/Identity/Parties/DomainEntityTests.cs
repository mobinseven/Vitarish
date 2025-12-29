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
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            ClientId = "client-id"
        };

        Assert.InRange(DateTime.UtcNow.Subtract(tenant.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Owner_SetsCreatedAtOnConstruction()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };

        Assert.InRange(DateTime.UtcNow.Subtract(owner.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TenantIndicator_SetsDefaults()
    {
        var indicator = new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Method = IndicatorMethod.Hostname,
            Value = "example.com"
        };

        Assert.Null(indicator.Verification);
        Assert.False(indicator.IsPrimary);
        Assert.InRange(DateTimeOffset.UtcNow.Subtract(indicator.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
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
