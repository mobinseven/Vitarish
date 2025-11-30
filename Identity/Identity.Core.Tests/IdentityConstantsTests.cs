using Vitarish.Identity.Core;

namespace Identity.Core.Tests;

public class IdentityConstantsTests
{
    [Fact]
    public void Admin_HasCorrectValue()
    {
        IdentityConstants.Admin.Should().Be("Admin");
    }

    [Fact]
    public void AccessClaim_HasCorrectValue()
    {
        IdentityConstants.AccessClaim.Should().Be("access");
    }

    [Fact]
    public void AccessScope_HasCorrectValue()
    {
        IdentityConstants.AccessScope.Should().Be("access");
    }

    [Fact]
    public void ClaimTypes_Subject_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.Subject.Should().Be("sub");
    }

    [Fact]
    public void ClaimTypes_ClientId_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.ClientId.Should().Be("client_id");
    }

    [Fact]
    public void ClaimTypes_GivenName_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.GivenName.Should().Be("given_name");
    }

    [Fact]
    public void ClaimTypes_FamilyName_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.FamilyName.Should().Be("family_name");
    }

    [Fact]
    public void ClaimTypes_Email_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.Email.Should().Be("email");
    }

    [Fact]
    public void ClaimTypes_TenantId_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.TenantId.Should().Be("tenant_id");
    }

    [Fact]
    public void ClaimTypes_OwnerId_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.OwnerId.Should().Be("owner_id");
    }

    [Fact]
    public void ClaimTypes_Name_HasCorrectValue()
    {
        IdentityConstants.ClaimTypes.Name.Should().Be("name");
    }
}
