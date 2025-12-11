using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Handlers;
using Vitarish.ApiClients.Tests.TestHelpers;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="GetApiClientHandler"/>.
/// </summary>
public sealed class GetApiClientHandlerTests : ApiClientDbContextTestBase
{

    [Fact]
    public async Task Handle_WithExistingClient_ReturnsClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = "test-client",
            Secret = "test-secret",
            Scopes = "read write",
            HostUri = "https://api.example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var query = new GetApiClient(Id: "test-client");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-client");
        result.Secret.Should().Be("test-secret");
        result.Scopes.Should().Be("read write");
        result.HostUri.Should().Be("https://api.example.com");
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_ReturnsNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientHandler(db, cache);

        var query = new GetApiClient(Id: "non-existing-client");

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
        var handler = new GetApiClientHandler(db, cache);

        // Add client to database
        var dbClient = new ApiClient
        {
            Id = "test-client",
            Secret = "db-secret",
            Scopes = "db-scopes",
            HostUri = "https://db.example.com"
        };
        db.ApiClients.Add(dbClient);
        await db.SaveChangesAsync();

        // First call - populates cache
        var query = new GetApiClient(Id: "test-client");
        await handler.Handle(query, CancellationToken.None);

        // Update database directly (simulating external change)
        dbClient.Secret = "updated-secret";
        await db.SaveChangesAsync();

        // Act - Second call should return cached value
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Should still have original secret from cache
        result.Should().NotBeNull();
        result!.Secret.Should().Be("db-secret");
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var query = new GetApiClient(Id: "test-client");

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
        var handler = new GetApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = id,
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var query = new GetApiClient(Id: id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }
}
