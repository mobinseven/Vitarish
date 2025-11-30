using System.Security.Claims;
using Vitarish.Identity.Core;
using Vitarish.Identity.Infrastructure.Context;

namespace Identity.Infrastructure.Tests.Context;

public class ClaimsIdentityContextTests
{
    [Fact]
    public void UserId_ReturnsSubjectClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.UserId.Should().Be("user-123");
    }

    [Fact]
    public void UserId_WhenNoSubjectClaim_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.UserId.Should().BeNull();
    }

    [Fact]
    public void OwnerId_ReturnsOwnerIdClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.OwnerId, "owner-456") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.OwnerId.Should().Be("owner-456");
    }

    [Fact]
    public void ClientId_ReturnsClientIdClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.ClientId, "client-789") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.ClientId.Should().Be("client-789");
    }

    [Fact]
    public void TenantId_ReturnsTenantIdClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.TenantId, "tenant-012") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.TenantId.Should().Be("tenant-012");
    }

    [Fact]
    public void IsAuthenticated_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        var principal = new ClaimsPrincipal(identity);
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void DisplayName_ReturnsNameClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Name, "John Doe") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void DisplayName_WhenNoName_ReturnsGivenAndFamilyName()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(IdentityConstants.ClaimTypes.GivenName, "John"),
            new Claim(IdentityConstants.ClaimTypes.FamilyName, "Doe")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void Email_ReturnsEmailClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Email, "john@example.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void HasClaim_WhenClaimExists_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim("role", "admin") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.HasClaim("role", "admin").Should().BeTrue();
    }

    [Fact]
    public void HasClaim_WhenClaimDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.HasClaim("role", "admin").Should().BeFalse();
    }

    [Fact]
    public void GetClaimValue_ReturnsClaimValue()
    {
        // Arrange
        var claims = new[] { new Claim("custom", "value123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.GetClaimValue("custom").Should().Be("value123");
    }

    [Fact]
    public void GetClaimValue_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsIdentityContext(principal);

        // Act & Assert
        context.GetClaimValue("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetClaimValues_ReturnsAllValues()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("role", "admin"),
            new Claim("role", "user"),
            new Claim("role", "editor")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsIdentityContext(principal);

        // Act
        var values = context.GetClaimValues("role").ToList();

        // Assert
        values.Should().HaveCount(3);
        values.Should().Contain("admin");
        values.Should().Contain("user");
        values.Should().Contain("editor");
    }

    [Fact]
    public void Constructor_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new ClaimsIdentityContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

