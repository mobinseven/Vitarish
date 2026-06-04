using Vitarish.Parties.Domain.Enums;

namespace Parties.Tests;

public class DomainEntityTests
{
    [Fact]
    public void Tenant_InitializesIndicatorsCollection()
    {
        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            AppId = "client-id"
        };

        Assert.NotNull(tenant.Indicators);
        Assert.Empty(tenant.Indicators);
    }

    [Fact]
    public void Tenant_SetsCreatedAtOnConstruction()
    {
        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Tenant",
            AppId = "client-id",
            CreatedAt = DateTime.UtcNow
        };

        Assert.InRange(DateTime.UtcNow.Subtract(tenant.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Owner_SetsCreatedAtOnConstruction()
    {
        Owner owner = new()
        {
            Id = Guid.NewGuid(),
            Name = "Owner",
            CreatedAt = DateTime.UtcNow
        };

        Assert.InRange(DateTime.UtcNow.Subtract(owner.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TenantIndicator_SetsDefaults()
    {
        TenantIndicator indicator = new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Method = IndicatorMethod.Hostname,
            Value = "example.com"
        };

        Assert.Null(indicator.Verification);
        Assert.False(indicator.IsPrimary);
        Assert.InRange(DateTime.UtcNow.Subtract(indicator.CreatedAt), TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void VerificationInfo_AllowsOptionalToken()
    {
        VerificationInfo info = new()
        {
            Method = IndicatorVerificationMethod.DnsTxt,
            Evidence = "_verify.example.com",
            EvidenceExpectedHash = "hash"
        };

        Assert.Null(info.Token);
        Assert.Equal(IndicatorVerificationMethod.DnsTxt, info.Method);
    }
}
