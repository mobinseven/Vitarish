using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Infrastructure.Handlers;
using Vitarish.AuthenticationClients.Tests.TestHelpers;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="GetAuthenticationClientHandler"/>.
/// </summary>
public sealed class GetAuthenticationClientHandlerTests : AuthenticationClientDbContextTestBase
{
    [Fact]
    public async Task Handle_WithExistingClient_ReturnsClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com",
            Title = "Test App",
            Description = "A test application",
            LogoUrl = "https://example.com/logo.png",
            AutoAccess = true
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClient(Id: "test-client");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-client");
        result.Url.Should().Be("https://example.com");
        result.Title.Should().Be("Test App");
        result.Description.Should().Be("A test application");
        result.LogoUrl.Should().Be("https://example.com/logo.png");
        result.AutoAccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_ReturnsNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        var query = new GetAuthenticationClient(Id: "non-existing-client");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCachedClient_ReturnsCachedValue()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        // First call to populate cache
        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com",
            Title = "Original Title"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClient(Id: "test-client");
        var firstResult = await handler.Handle(query, CancellationToken.None);

        // Update the database directly (simulating external change)
        existingClient.Title = "Updated Title";
        await db.SaveChangesAsync();

        // Act - Second call should return cached value
        var secondResult = await handler.Handle(query, CancellationToken.None);

        // Assert - Should get the cached original value
        secondResult.Should().NotBeNull();
        secondResult!.Title.Should().Be("Original Title");
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var query = new GetAuthenticationClient(Id: "test-client");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client.with.dots")]
    public async Task Handle_WithVariousIdFormats_ReturnsCorrectClient(string id)
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        var existingClient = new AuthenticationClient
        {
            Id = id,
            Url = "https://example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClient(Id: id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task Handle_WithMultipleClients_ReturnsCorrectOne()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientHandler(db, cache);

        db.AuthenticationClients.AddRange(
            new AuthenticationClient { Id = "client-1", Url = "https://example1.com", Title = "App 1" },
            new AuthenticationClient { Id = "client-2", Url = "https://example2.com", Title = "App 2" },
            new AuthenticationClient { Id = "client-3", Url = "https://example3.com", Title = "App 3" }
        );
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClient(Id: "client-2");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("client-2");
        result.Title.Should().Be("App 2");
    }
}
