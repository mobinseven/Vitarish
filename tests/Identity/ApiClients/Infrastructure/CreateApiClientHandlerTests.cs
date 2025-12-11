using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Domain.Events;
using Vitarish.ApiClients.Infrastructure.Handlers;
using Vitarish.ApiClients.Tests.TestHelpers;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="CreateApiClientHandler"/>.
/// </summary>
public sealed class CreateApiClientHandlerTests : ApiClientDbContextTestBase
{

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndReturnsApiClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        var command = new CreateApiClient(
            Id: "test-client",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-client");
        result.Secret.Should().Be("test-secret");
        result.Scopes.Should().Be("read write");
        result.HostUri.Should().Be("https://api.example.com");
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsToDatabase()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        var command = new CreateApiClient(
            Id: "test-client",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Use new context to verify persistence
        await using var assertDb = CreateInMemoryDbContext();
        var persisted = await assertDb.ApiClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        persisted.Should().NotBeNull();
        persisted!.Secret.Should().Be("test-secret");
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesEvent()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        var command = new CreateApiClient(
            Id: "test-client",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        publisher.PublishedNotifications.Should().ContainSingle();
        var @event = publisher.PublishedNotifications[0].Should().BeOfType<ApiClientCreatedEvent>().Subject;
        @event.ClientId.Should().Be("test-client");
    }

    [Fact]
    public async Task Handle_WithDuplicateId_ReturnsNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        // Create first client
        var existingClient = new ApiClient
        {
            Id = "existing-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new CreateApiClient(
            Id: "existing-client",
            Secret: "new-secret",
            Scopes: "new-scopes",
            HostUri: "https://new.example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDuplicateId_DoesNotPublishEvent()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        // Create first client
        var existingClient = new ApiClient
        {
            Id = "existing-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new CreateApiClient(
            Id: "existing-client",
            Secret: "new-secret",
            Scopes: "new-scopes",
            HostUri: "https://new.example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        publisher.PublishedNotifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateApiClientHandler(db, publisher, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new CreateApiClient(
            Id: "test-client",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }
}
