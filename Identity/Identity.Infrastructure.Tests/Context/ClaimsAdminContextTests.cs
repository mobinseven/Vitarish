using System.Security.Claims;
using Vitarish.Identity.Core;
using Vitarish.Identity.Infrastructure.Context;

namespace Identity.Infrastructure.Tests.Context;

public class ClaimsAdminContextTests
{
    [Fact]
    public void UserId_ReturnsSubjectClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "admin-user") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.UserId.Should().Be("admin-user");
    }

    [Fact]
    public void UserId_WhenNoSubjectClaim_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.UserId.Should().BeNull();
    }

    [Fact]
    public void OwnerId_ReturnsOwnerIdClaim()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.OwnerId, "owner-admin") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.OwnerId.Should().Be("owner-admin");
    }

    [Fact]
    public void IsAdmin_WhenInAdminRole_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, IdentityConstants.Admin));
        var principal = new ClaimsPrincipal(identity);
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_WhenNotInAdminRole_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        var principal = new ClaimsPrincipal(identity);
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WhenNoRoles_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new ClaimsAdminContext(principal);

        // Act & Assert
        context.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new ClaimsAdminContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
