namespace Vitarish.ApiClients.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="ApiClient"/> entity.
/// </summary>
public sealed class ApiClientTests
{
    [Fact]
    public void ApiClient_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        var client = new ApiClient
        {
            Id = "test-client-id",
            Secret = "test-secret",
            Scopes = "read write",
            HostUri = "https://api.example.com"
        };

        // Assert
        client.Id.Should().Be("test-client-id");
        client.Secret.Should().Be("test-secret");
        client.Scopes.Should().Be("read write");
        client.HostUri.Should().Be("https://api.example.com");
    }

    [Fact]
    public void ApiClient_PropertiesAreSettable()
    {
        // Arrange
        var client = new ApiClient
        {
            Id = "initial-id",
            Secret = "initial-secret",
            Scopes = "initial-scopes",
            HostUri = "https://initial.example.com"
        };

        // Act
        client.Secret = "updated-secret";
        client.Scopes = "updated-scopes";
        client.HostUri = "https://updated.example.com";

        // Assert
        client.Secret.Should().Be("updated-secret");
        client.Scopes.Should().Be("updated-scopes");
        client.HostUri.Should().Be("https://updated.example.com");
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    [InlineData("client.with.dots")]
    public void ApiClient_AcceptsVariousIdFormats(string id)
    {
        // Arrange & Act
        var client = new ApiClient
        {
            Id = id,
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };

        // Assert
        client.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("read write")]
    [InlineData("read write admin")]
    [InlineData("api.read api.write")]
    public void ApiClient_AcceptsVariousScopeFormats(string scopes)
    {
        // Arrange & Act
        var client = new ApiClient
        {
            Id = "test-id",
            Secret = "secret",
            Scopes = scopes,
            HostUri = "https://example.com"
        };

        // Assert
        client.Scopes.Should().Be(scopes);
    }

    [Theory]
    [InlineData("https://api.example.com")]
    [InlineData("https://api.example.com:8080")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://api.example.com/v1")]
    public void ApiClient_AcceptsVariousHostUriFormats(string hostUri)
    {
        // Arrange & Act
        var client = new ApiClient
        {
            Id = "test-id",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = hostUri
        };

        // Assert
        client.HostUri.Should().Be(hostUri);
    }
}
