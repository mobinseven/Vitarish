using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Handlers;
using Vitarish.ApiClients.Tests.TestHelpers;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="GetApiClientsHandler"/>.
/// </summary>
public sealed class GetApiClientsHandlerTests : ApiClientDbContextTestBase
{

    [Fact]
    public async Task Handle_WithNoClients_ReturnsEmptyCollection()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientsHandler(db, cache);

        var query = new GetApiClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSingleClient_ReturnsClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientsHandler(db, cache);

        var client = new ApiClient
        {
            Id = "test-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(client);
        await db.SaveChangesAsync();

        var query = new GetApiClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("test-client");
    }

    [Fact]
    public async Task Handle_WithMultipleClients_ReturnsAllClients()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientsHandler(db, cache);

        var clients = new[]
        {
            new ApiClient { Id = "client-1", Secret = "secret-1", Scopes = "scopes-1", HostUri = "https://example1.com" },
            new ApiClient { Id = "client-2", Secret = "secret-2", Scopes = "scopes-2", HostUri = "https://example2.com" },
            new ApiClient { Id = "client-3", Secret = "secret-3", Scopes = "scopes-3", HostUri = "https://example3.com" }
        };
        db.ApiClients.AddRange(clients);
        await db.SaveChangesAsync();

        var query = new GetApiClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Id).Should().Contain("client-1", "client-2", "client-3");
    }

    [Fact]
    public async Task Handle_WithCachedClients_ReturnsCachedValue()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientsHandler(db, cache);

        // Add clients to database
        var client = new ApiClient
        {
            Id = "test-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(client);
        await db.SaveChangesAsync();

        // First call - populates cache
        var query = new GetApiClients();
        await handler.Handle(query, CancellationToken.None);

        // Add another client directly to database (simulating external change)
        var newClient = new ApiClient
        {
            Id = "new-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://new.example.com"
        };
        db.ApiClients.Add(newClient);
        await db.SaveChangesAsync();

        // Act - Second call should return cached value
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Should only have original client from cache
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("test-client");
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetApiClientsHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var query = new GetApiClients();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }
}
