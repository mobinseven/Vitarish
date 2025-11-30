using Vitarish.Identity.Core.Domain.Entities;

namespace Identity.Core.Tests.Domain.Entities;

public class AuthenticationClientTests
{
    [Fact]
    public void AuthenticationClient_WhenCreated_HasDefaultValues()
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com"
        };

        // Assert
        client.AutoAccess.Should().BeTrue();
        client.Title.Should().BeNull();
        client.Description.Should().BeNull();
        client.LogoUrl.Should().BeNull();
        client.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        client.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void AuthenticationClient_RequiredProperties_AreSet()
    {
        // Arrange
        var id = "my-client";
        var url = "https://myapp.example.com";

        // Act
        var client = new AuthenticationClient
        {
            Id = id,
            Url = url
        };

        // Assert
        client.Id.Should().Be(id);
        client.Url.Should().Be(url);
    }

    [Fact]
    public void AuthenticationClient_AllProperties_CanBeSet()
    {
        // Arrange
        var id = "full-client";
        var url = "https://full.example.com";
        var title = "Full Client";
        var description = "A fully configured client";
        var logoUrl = "https://full.example.com/logo.png";
        var autoAccess = false;

        // Act
        var client = new AuthenticationClient
        {
            Id = id,
            Url = url,
            Title = title,
            Description = description,
            LogoUrl = logoUrl,
            AutoAccess = autoAccess
        };

        // Assert
        client.Id.Should().Be(id);
        client.Url.Should().Be(url);
        client.Title.Should().Be(title);
        client.Description.Should().Be(description);
        client.LogoUrl.Should().Be(logoUrl);
        client.AutoAccess.Should().BeFalse();
    }

    [Fact]
    public void AuthenticationClient_UpdatedAt_CanBeSet()
    {
        // Arrange
        var client = new AuthenticationClient
        {
            Id = "updatable-client",
            Url = "https://updatable.example.com"
        };
        var updateTime = DateTimeOffset.UtcNow;

        // Act
        client.UpdatedAt = updateTime;

        // Assert
        client.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void AuthenticationClient_AutoAccess_DefaultsToTrue()
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "auto-client",
            Url = "https://auto.example.com"
        };

        // Assert
        client.AutoAccess.Should().BeTrue();
    }

    [Fact]
    public void AuthenticationClient_AutoAccess_CanBeSetToFalse()
    {
        // Arrange
        var client = new AuthenticationClient
        {
            Id = "no-auto-client",
            Url = "https://noauto.example.com",
            AutoAccess = false
        };

        // Assert
        client.AutoAccess.Should().BeFalse();
    }
}
