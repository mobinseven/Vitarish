using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Enums;

namespace Vitarish.Identity.Core.Tests.Domain;

/// <summary>
/// Tests for TenantIndicator entity.
/// </summary>
public class TenantIndicatorTests
{
    [Fact]
    public void TenantIndicator_CreatesWithDefaults()
    {
        var indicator = new TenantIndicator
        {
            Value = "example.com"
        };

        Assert.Equal(Guid.Empty, indicator.Id);
        Assert.Equal(Guid.Empty, indicator.TenantId);
        Assert.Equal(default, indicator.Method);
        Assert.Equal(default, indicator.Status);
        Assert.False(indicator.IsPrimary);
        Assert.Null(indicator.Verification);
        Assert.True(indicator.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(indicator.VerifiedAt);
        Assert.Null(indicator.LastCheckedAt);
    }

    [Fact]
    public void TenantIndicator_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var indicator = new TenantIndicator
        {
            Id = indicatorId,
            TenantId = tenantId,
            Method = IndicatorMethod.Subdomain,
            Value = "customer.saas.example.com",
            Status = IndicatorStatus.Active,
            IsPrimary = true,
            Verification = new VerificationInfo
            {
                Method = IndicatorVerificationMethod.DnsTxt,
                Evidence = "_verify.customer.saas.example.com",
                EvidenceExpectedHash = "abc123hash"
            },
            CreatedAt = now,
            VerifiedAt = now.AddMinutes(5),
            LastCheckedAt = now.AddMinutes(10)
        };

        Assert.Equal(indicatorId, indicator.Id);
        Assert.Equal(tenantId, indicator.TenantId);
        Assert.Equal(IndicatorMethod.Subdomain, indicator.Method);
        Assert.Equal("customer.saas.example.com", indicator.Value);
        Assert.Equal(IndicatorStatus.Active, indicator.Status);
        Assert.True(indicator.IsPrimary);
        Assert.NotNull(indicator.Verification);
        Assert.Equal(IndicatorVerificationMethod.DnsTxt, indicator.Verification.Method);
        Assert.Equal("_verify.customer.saas.example.com", indicator.Verification.Evidence);
        Assert.Equal("abc123hash", indicator.Verification.EvidenceExpectedHash);
    }
}

/// <summary>
/// Tests for Tenant entity.
/// </summary>
public class TenantTests
{
    [Fact]
    public void Tenant_CreatesWithDefaults()
    {
        var tenant = new Tenant
        {
            Name = "Test Tenant",
            ClientId = "test-client"
        };

        Assert.Equal(Guid.Empty, tenant.Id);
        Assert.Equal(Guid.Empty, tenant.OwnerId);
        Assert.Null(tenant.LogoUrl);
        Assert.Null(tenant.Description);
        Assert.True(tenant.CreatedAt <= DateTime.UtcNow);
        Assert.Null(tenant.CreatedBy);
        Assert.Null(tenant.ModifiedAt);
        Assert.Null(tenant.ModifiedBy);
        Assert.Empty(tenant.Indicators);
    }

    [Fact]
    public void Tenant_IndicatorsCollection_CanBeModified()
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
            Value = "app.customer.com",
            Status = IndicatorStatus.Active
        };

        tenant.Indicators.Add(indicator);

        Assert.Single(tenant.Indicators);
        Assert.Equal(indicator, tenant.Indicators.First());
    }
}

/// <summary>
/// Tests for Owner entity.
/// </summary>
public class OwnerTests
{
    [Fact]
    public void Owner_CreatesWithDefaults()
    {
        var owner = new Owner
        {
            Name = "Test Owner"
        };

        Assert.Equal(Guid.Empty, owner.Id);
        Assert.Null(owner.LogoUrl);
        Assert.Null(owner.Description);
        Assert.True(owner.CreatedAt <= DateTime.UtcNow);
        Assert.Null(owner.CreatedBy);
        Assert.Null(owner.ModifiedAt);
        Assert.Null(owner.ModifiedBy);
    }

    [Fact]
    public void Owner_SetsAllProperties()
    {
        var ownerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var modifiedAt = createdAt.AddHours(1);

        var owner = new Owner
        {
            Id = ownerId,
            Name = "Acme Corporation",
            LogoUrl = "https://example.com/logo.png",
            Description = "A test owner",
            CreatedAt = createdAt,
            CreatedBy = "admin",
            ModifiedAt = modifiedAt,
            ModifiedBy = "editor"
        };

        Assert.Equal(ownerId, owner.Id);
        Assert.Equal("Acme Corporation", owner.Name);
        Assert.Equal("https://example.com/logo.png", owner.LogoUrl);
        Assert.Equal("A test owner", owner.Description);
        Assert.Equal(createdAt, owner.CreatedAt);
        Assert.Equal("admin", owner.CreatedBy);
        Assert.Equal(modifiedAt, owner.ModifiedAt);
        Assert.Equal("editor", owner.ModifiedBy);
    }
}

/// <summary>
/// Tests for VerificationInfo value object.
/// </summary>
public class VerificationInfoTests
{
    [Fact]
    public void VerificationInfo_CreatesWithRequiredProperties()
    {
        var verification = new VerificationInfo
        {
            Method = IndicatorVerificationMethod.DnsTxt,
            Evidence = "_verify.example.com",
            EvidenceExpectedHash = "abc123hash"
        };

        Assert.Equal(IndicatorVerificationMethod.DnsTxt, verification.Method);
        Assert.Equal("_verify.example.com", verification.Evidence);
        Assert.Equal("abc123hash", verification.EvidenceExpectedHash);
        Assert.Null(verification.Token);
    }

    [Fact]
    public void VerificationInfo_TokenIsOptional()
    {
        var verification = new VerificationInfo
        {
            Method = IndicatorVerificationMethod.DnsTxt,
            Evidence = "_verify.example.com",
            EvidenceExpectedHash = "abc123hash",
            Token = "verify=secret-token"
        };

        Assert.Equal("verify=secret-token", verification.Token);
    }
}

/// <summary>
/// Tests for User entity.
/// </summary>
public class UserTests
{
    [Fact]
    public void User_CreatesWithDefaults()
    {
        var user = new User
        {
            UserName = "testuser"
        };

        Assert.NotNull(user.Id);
        Assert.NotEqual(Guid.Empty.ToString(), user.Id);
        Assert.Null(user.Email);
        Assert.False(user.EmailConfirmed);
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
        Assert.Null(user.ProfileJson);
        Assert.Empty(user.ProfilePreferences);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
        Assert.Null(user.ModifiedAt);
    }

    [Fact]
    public void User_ProfilePreferences_SerializesAndDeserializes()
    {
        var user = new User
        {
            UserName = "testuser"
        };

        user.ProfilePreferences = new Dictionary<string, string>
        {
            ["theme"] = "dark",
            ["language"] = "en"
        };

        Assert.NotNull(user.ProfileJson);
        Assert.Contains("theme", user.ProfileJson);
        Assert.Contains("dark", user.ProfileJson);

        // Verify round-trip
        var retrieved = user.ProfilePreferences;
        Assert.Equal(2, retrieved.Count);
        Assert.Equal("dark", retrieved["theme"]);
        Assert.Equal("en", retrieved["language"]);
    }

    [Fact]
    public void User_ProfilePreferences_HandlesEmptyDictionary()
    {
        var user = new User
        {
            UserName = "testuser"
        };

        user.ProfilePreferences = new Dictionary<string, string>();

        Assert.Null(user.ProfileJson);
        Assert.Empty(user.ProfilePreferences);
    }

    [Fact]
    public void User_ProfilePreferences_HandlesNull()
    {
        var user = new User
        {
            UserName = "testuser",
            ProfileJson = null
        };

        Assert.Empty(user.ProfilePreferences);
    }
}

/// <summary>
/// Tests for AuthenticationClient entity.
/// </summary>
public class AuthenticationClientTests
{
    [Fact]
    public void AuthenticationClient_CreatesWithDefaults()
    {
        var client = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com"
        };

        Assert.Equal("test-client", client.Id);
        Assert.Equal("https://example.com", client.Url);
        Assert.Null(client.Title);
        Assert.Null(client.Description);
        Assert.Null(client.LogoUrl);
        Assert.True(client.AutoAccess);
    }

    [Fact]
    public void AuthenticationClient_SetsAllProperties()
    {
        var client = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com",
            Title = "Test Application",
            Description = "A test application",
            LogoUrl = "https://example.com/logo.png",
            AutoAccess = false
        };

        Assert.Equal("test-client", client.Id);
        Assert.Equal("https://example.com", client.Url);
        Assert.Equal("Test Application", client.Title);
        Assert.Equal("A test application", client.Description);
        Assert.Equal("https://example.com/logo.png", client.LogoUrl);
        Assert.False(client.AutoAccess);
    }
}
