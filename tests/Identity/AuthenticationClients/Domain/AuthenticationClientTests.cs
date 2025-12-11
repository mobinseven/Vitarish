namespace Vitarish.AuthenticationClients.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="AuthenticationClient"/> entity.
/// </summary>
public sealed class AuthenticationClientTests
{
    [Fact]
    public void AuthenticationClient_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-client-id",
            Url = "https://app.example.com"
        };

        // Assert
        client.Id.Should().Be("test-client-id");
        client.Url.Should().Be("https://app.example.com");
    }

    [Fact]
    public void AuthenticationClient_PropertiesAreSettable()
    {
        // Arrange
        var client = new AuthenticationClient
        {
            Id = "initial-id",
            Url = "https://initial.example.com"
        };

        // Act
        client.Url = "https://updated.example.com";
        client.Title = "Updated Title";
        client.Description = "Updated Description";
        client.LogoUrl = "https://example.com/logo.png";
        client.AutoAccess = false;

        // Assert
        client.Url.Should().Be("https://updated.example.com");
        client.Title.Should().Be("Updated Title");
        client.Description.Should().Be("Updated Description");
        client.LogoUrl.Should().Be("https://example.com/logo.png");
        client.AutoAccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    [InlineData("client.with.dots")]
    public void AuthenticationClient_AcceptsVariousIdFormats(string id)
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = id,
            Url = "https://example.com"
        };

        // Assert
        client.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("https://app.example.com")]
    [InlineData("https://app.example.com:8080")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://app.example.com/callback")]
    public void AuthenticationClient_AcceptsVariousUrlFormats(string url)
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = url
        };

        // Assert
        client.Url.Should().Be(url);
    }

    [Fact]
    public void AuthenticationClient_AutoAccessDefaultsToTrue()
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com"
        };

        // Assert
        client.AutoAccess.Should().BeTrue();
    }

    [Fact]
    public void AuthenticationClient_OptionalPropertiesAreNullByDefault()
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com"
        };

        // Assert
        client.Title.Should().BeNull();
        client.Description.Should().BeNull();
        client.LogoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("My App")]
    [InlineData("")]
    [InlineData("App with special chars: !@#$%")]
    public void AuthenticationClient_AcceptsVariousTitleFormats(string? title)
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com",
            Title = title
        };

        // Assert
        client.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("A description of the application")]
    [InlineData("")]
    [InlineData("Multi-line\ndescription\ntext")]
    public void AuthenticationClient_AcceptsVariousDescriptionFormats(string? description)
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com",
            Description = description
        };

        // Assert
        client.Description.Should().Be(description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AuthenticationClient_AutoAccessCanBeSetToAnyBoolValue(bool autoAccess)
    {
        // Arrange & Act
        var client = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com",
            AutoAccess = autoAccess
        };

        // Assert
        client.AutoAccess.Should().Be(autoAccess);
    }
}
