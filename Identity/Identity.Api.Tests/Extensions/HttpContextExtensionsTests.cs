using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vitarish.Identity.Api.Extensions;
using Vitarish.Identity.Core;
using Vitarish.Identity.Core.Abstractions;

namespace Identity.Api.Tests.Extensions;

public class HttpContextExtensionsTests
{
    [Fact]
    public void GetIdentityContext_ReturnsIdentityContext()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "user-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Act
        var context = httpContext.GetIdentityContext();

        // Assert
        context.Should().NotBeNull();
        context.UserId.Should().Be("user-123");
    }

    [Fact]
    public void GetAdminContext_ReturnsAdminContext()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(IdentityConstants.ClaimTypes.Subject, "admin-123"),
            new Claim(ClaimTypes.Role, IdentityConstants.Admin)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Act
        var context = httpContext.GetAdminContext();

        // Assert
        context.Should().NotBeNull();
        context.UserId.Should().Be("admin-123");
        context.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void GetUserId_ReturnsUserIdFromContext()
    {
        // Arrange
        var claims = new[] { new Claim(IdentityConstants.ClaimTypes.Subject, "user-456") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Act
        var userId = httpContext.GetUserId();

        // Assert
        userId.Should().Be("user-456");
    }

    [Fact]
    public void GetUserId_WhenNoUser_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var userId = httpContext.GetUserId();

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void IsUserAuthenticated_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var httpContext = new DefaultHttpContext { User = principal };

        // Act
        var isAuthenticated = httpContext.IsUserAuthenticated();

        // Assert
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsUserAuthenticated_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var isAuthenticated = httpContext.IsUserAuthenticated();

        // Assert
        isAuthenticated.Should().BeFalse();
    }
}
