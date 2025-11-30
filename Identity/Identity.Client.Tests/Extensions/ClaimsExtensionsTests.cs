using System.Security.Claims;
using Vitarish.Identity.Client.Extensions;
using Vitarish.Identity.Contracts.Dtos;
using Vitarish.Identity.Core;

namespace Identity.Client.Tests.Extensions;

public class ClaimsExtensionsTests
{
    #region GetUserId Tests

    [Fact]
    public void GetUserId_FromPrincipal_ReturnsSubjectClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var userId = principal.GetUserId();

        // Assert
        userId.Should().Be("user-123");
    }

    [Fact]
    public void GetUserId_FromPrincipal_WhenMissing_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var userId = principal.GetUserId();

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserId_FromClaimDtos_ReturnsSubjectClaim()
    {
        // Arrange
        var claims = new List<ClaimDto>
        {
            new(IdentityConstants.ClaimTypes.Subject, "user-456")
        };

        // Act
        var userId = claims.GetUserId();

        // Assert
        userId.Should().Be("user-456");
    }

    [Fact]
    public void GetUserId_FromClaimDtos_WhenEmpty_ReturnsNull()
    {
        // Arrange
        var claims = new List<ClaimDto>();

        // Act
        var userId = claims.GetUserId();

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region GetGivenName Tests

    [Fact]
    public void GetGivenName_FromPrincipal_ReturnsGivenNameClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.GivenName, "John") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var givenName = principal.GetGivenName();

        // Assert
        givenName.Should().Be("John");
    }

    [Fact]
    public void GetGivenName_FromClaimDtos_ReturnsGivenNameClaim()
    {
        // Arrange
        var claims = new List<ClaimDto>
        {
            new(IdentityConstants.ClaimTypes.GivenName, "Jane")
        };

        // Act
        var givenName = claims.GetGivenName();

        // Assert
        givenName.Should().Be("Jane");
    }

    #endregion

    #region GetFamilyName Tests

    [Fact]
    public void GetFamilyName_FromPrincipal_ReturnsFamilyNameClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.FamilyName, "Doe") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var familyName = principal.GetFamilyName();

        // Assert
        familyName.Should().Be("Doe");
    }

    [Fact]
    public void GetFamilyName_FromClaimDtos_ReturnsFamilyNameClaim()
    {
        // Arrange
        var claims = new List<ClaimDto>
        {
            new(IdentityConstants.ClaimTypes.FamilyName, "Smith")
        };

        // Act
        var familyName = claims.GetFamilyName();

        // Assert
        familyName.Should().Be("Smith");
    }

    #endregion

    #region GetEmail Tests

    [Fact]
    public void GetEmail_FromPrincipal_ReturnsEmailClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Email, "john@example.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var email = principal.GetEmail();

        // Assert
        email.Should().Be("john@example.com");
    }

    [Fact]
    public void GetEmail_FromClaimDtos_ReturnsEmailClaim()
    {
        // Arrange
        var claims = new List<ClaimDto>
        {
            new(IdentityConstants.ClaimTypes.Email, "jane@example.com")
        };

        // Act
        var email = claims.GetEmail();

        // Assert
        email.Should().Be("jane@example.com");
    }

    #endregion

    #region GetClientId Tests

    [Fact]
    public void GetClientId_FromPrincipal_ReturnsClientIdClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.ClientId, "my-client") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var clientId = principal.GetClientId();

        // Assert
        clientId.Should().Be("my-client");
    }

    #endregion

    #region GetDisplayName Tests

    [Fact]
    public void GetDisplayName_WhenNameClaimExists_ReturnsNameClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(IdentityConstants.ClaimTypes.Name, "Full Name"),
            new Claim(IdentityConstants.ClaimTypes.GivenName, "Given"),
            new Claim(IdentityConstants.ClaimTypes.FamilyName, "Family")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var displayName = principal.GetDisplayName();

        // Assert
        displayName.Should().Be("Full Name");
    }

    [Fact]
    public void GetDisplayName_WhenNoNameClaim_ReturnsGivenAndFamilyName()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(IdentityConstants.ClaimTypes.GivenName, "John"),
            new Claim(IdentityConstants.ClaimTypes.FamilyName, "Doe")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var displayName = principal.GetDisplayName();

        // Assert
        displayName.Should().Be("John Doe");
    }

    [Fact]
    public void GetDisplayName_WhenOnlyGivenName_ReturnsGivenName()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.GivenName, "John") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var displayName = principal.GetDisplayName();

        // Assert
        displayName.Should().Be("John");
    }

    [Fact]
    public void GetDisplayName_WhenNoClaims_ReturnsEmpty()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var displayName = principal.GetDisplayName();

        // Assert
        displayName.Should().BeEmpty();
    }

    #endregion

    #region HasAccess Tests

    [Fact]
    public void HasAccess_WhenAccessClaimExists_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.AccessClaim, "admin") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasAccess = principal.HasAccess("admin");

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public void HasAccess_WhenAccessClaimDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.AccessClaim, "user") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var hasAccess = principal.HasAccess("admin");

        // Assert
        hasAccess.Should().BeFalse();
    }

    [Fact]
    public void HasAccess_WhenNoAccessClaims_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var hasAccess = principal.HasAccess("admin");

        // Assert
        hasAccess.Should().BeFalse();
    }

    #endregion

    #region GetAccessValues Tests

    [Fact]
    public void GetAccessValues_ReturnsAllAccessClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(IdentityConstants.AccessClaim, "admin"),
            new Claim(IdentityConstants.AccessClaim, "editor"),
            new Claim(IdentityConstants.AccessClaim, "viewer")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var accessValues = principal.GetAccessValues().ToList();

        // Assert
        accessValues.Should().HaveCount(3);
        accessValues.Should().Contain("admin");
        accessValues.Should().Contain("editor");
        accessValues.Should().Contain("viewer");
    }

    [Fact]
    public void GetAccessValues_WhenNoAccessClaims_ReturnsEmpty()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var accessValues = principal.GetAccessValues().ToList();

        // Assert
        accessValues.Should().BeEmpty();
    }

    #endregion
}
