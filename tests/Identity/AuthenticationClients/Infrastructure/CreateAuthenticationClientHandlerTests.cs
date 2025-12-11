using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Domain.Events;
using Vitarish.AuthenticationClients.Infrastructure.Handlers;
using Vitarish.AuthenticationClients.Tests.TestHelpers;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="CreateAuthenticationClientHandler"/>.
/// </summary>
public sealed class CreateAuthenticationClientHandlerTests : AuthenticationClientDbContextTestBase
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndReturnsAuthenticationClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        var command = new CreateAuthenticationClient(
            Id: "test-client",
            Url: "https://app.example.com",
            Title: "Test App",
            Description: "A test application",
            LogoUrl: "https://example.com/logo.png",
            AutoAccess: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-client");
        result.Url.Should().Be("https://app.example.com");
        result.Title.Should().Be("Test App");
        result.Description.Should().Be("A test application");
        result.LogoUrl.Should().Be("https://example.com/logo.png");
        result.AutoAccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsToDatabase()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        var command = new CreateAuthenticationClient(
            Id: "test-client",
            Url: "https://app.example.com",
            Title: "Test App",
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Use new context to verify persistence
        await using var assertDb = CreateInMemoryDbContext();
        var persisted = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        persisted.Should().NotBeNull();
        persisted!.Url.Should().Be("https://app.example.com");
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesEvent()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        var command = new CreateAuthenticationClient(
            Id: "test-client",
            Url: "https://app.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        publisher.PublishedNotifications.Should().ContainSingle();
        var @event = publisher.PublishedNotifications[0].Should().BeOfType<AuthenticationClientCreatedEvent>().Subject;
        @event.ClientId.Should().Be("test-client");
    }

    [Fact]
    public async Task Handle_WithDuplicateId_ReturnsNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        // Create first client
        var existingClient = new AuthenticationClient
        {
            Id = "existing-client",
            Url = "https://example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new CreateAuthenticationClient(
            Id: "existing-client",
            Url: "https://new.example.com",
            Title: "New Title",
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

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
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        // Create first client
        var existingClient = new AuthenticationClient
        {
            Id = "existing-client",
            Url = "https://example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new CreateAuthenticationClient(
            Id: "existing-client",
            Url: "https://new.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

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
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new CreateAuthenticationClient(
            Id: "test-client",
            Url: "https://app.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_WithMinimalFields_CreatesClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var publisher = new FakePublisher();
        var handler = new CreateAuthenticationClientHandler(db, publisher, cache);

        var command = new CreateAuthenticationClient(
            Id: "minimal-client",
            Url: "https://minimal.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("minimal-client");
        result.Title.Should().BeNull();
        result.Description.Should().BeNull();
        result.LogoUrl.Should().BeNull();
        result.AutoAccess.Should().BeFalse();
    }
}
