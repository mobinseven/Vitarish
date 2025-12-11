using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Handlers;
using Vitarish.ApiClients.Tests.TestHelpers;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="UpdateApiClientHandler"/>.
/// </summary>
public sealed class UpdateApiClientHandlerTests : ApiClientDbContextTestBase
{

    [Fact]
    public async Task Handle_WithExistingClient_UpdatesAndReturnsClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = "test-client",
            Secret = "original-secret",
            Scopes = "original-scopes",
            HostUri = "https://original.example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateApiClient(
            Id: "test-client",
            Secret: "updated-secret",
            Scopes: "updated-scopes",
            HostUri: "https://updated.example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-client");
        result.Secret.Should().Be("updated-secret");
        result.Scopes.Should().Be("updated-scopes");
        result.HostUri.Should().Be("https://updated.example.com");
    }

    [Fact]
    public async Task Handle_WithExistingClient_PersistsUpdatesToDatabase()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = "test-client",
            Secret = "original-secret",
            Scopes = "original-scopes",
            HostUri = "https://original.example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateApiClient(
            Id: "test-client",
            Secret: "updated-secret",
            Scopes: "updated-scopes",
            HostUri: "https://updated.example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Use new context to verify persistence
        await using var assertDb = CreateInMemoryDbContext();
        var persisted = await assertDb.ApiClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        persisted.Should().NotBeNull();
        persisted!.Secret.Should().Be("updated-secret");
        persisted.Scopes.Should().Be("updated-scopes");
        persisted.HostUri.Should().Be("https://updated.example.com");
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_ReturnsNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        var command = new UpdateApiClient(
            Id: "non-existing-client",
            Secret: "updated-secret",
            Scopes: "updated-scopes",
            HostUri: "https://updated.example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_DoesNotCreateNewClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        var command = new UpdateApiClient(
            Id: "non-existing-client",
            Secret: "updated-secret",
            Scopes: "updated-scopes",
            HostUri: "https://updated.example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var count = await db.ApiClients.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new UpdateApiClient(
            Id: "test-client",
            Secret: "secret",
            Scopes: "scopes",
            HostUri: "https://example.com");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_PreservesClientId()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = "original-id",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateApiClient(
            Id: "original-id",
            Secret: "new-secret",
            Scopes: "new-scopes",
            HostUri: "https://new.example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result!.Id.Should().Be("original-id");
    }
}
