using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Infrastructure.Handlers;
using Vitarish.AuthenticationClients.Tests.TestHelpers;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="GetAuthenticationClientsHandler"/>.
/// </summary>
public sealed class GetAuthenticationClientsHandlerTests : AuthenticationClientDbContextTestBase
{
    [Fact]
    public async Task Handle_WithNoClients_ReturnsEmptyCollection()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientsHandler(db, cache);

        var query = new GetAuthenticationClients();

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
        var handler = new GetAuthenticationClientsHandler(db, cache);

        var client = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com",
            Title = "Test App"
        };
        db.AuthenticationClients.Add(client);
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result.First().Id.Should().Be("test-client");
    }

    [Fact]
    public async Task Handle_WithMultipleClients_ReturnsAllClients()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientsHandler(db, cache);

        db.AuthenticationClients.AddRange(
            new AuthenticationClient { Id = "client-1", Url = "https://example1.com", Title = "App 1" },
            new AuthenticationClient { Id = "client-2", Url = "https://example2.com", Title = "App 2" },
            new AuthenticationClient { Id = "client-3", Url = "https://example3.com", Title = "App 3" }
        );
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(c => c.Id).Should().Contain(["client-1", "client-2", "client-3"]);
    }

    [Fact]
    public async Task Handle_WithCachedClients_ReturnsCachedValue()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientsHandler(db, cache);

        // First call to populate cache
        db.AuthenticationClients.Add(new AuthenticationClient { Id = "client-1", Url = "https://example.com" });
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClients();
        var firstResult = await handler.Handle(query, CancellationToken.None);

        // Add another client to database directly (simulating external change)
        db.AuthenticationClients.Add(new AuthenticationClient { Id = "client-2", Url = "https://example2.com" });
        await db.SaveChangesAsync();

        // Act - Second call should return cached value
        var secondResult = await handler.Handle(query, CancellationToken.None);

        // Assert - Should get the cached original value (only 1 client)
        secondResult.Should().NotBeNull();
        secondResult.Should().ContainSingle();
        secondResult.First().Id.Should().Be("client-1");
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientsHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var query = new GetAuthenticationClients();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    [Fact]
    public async Task Handle_ReturnsClientsWithAllProperties()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new GetAuthenticationClientsHandler(db, cache);

        db.AuthenticationClients.Add(new AuthenticationClient
        {
            Id = "full-client",
            Url = "https://full.example.com",
            Title = "Full App",
            Description = "A fully populated client",
            LogoUrl = "https://example.com/logo.png",
            AutoAccess = false
        });
        await db.SaveChangesAsync();

        var query = new GetAuthenticationClients();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var client = result.First();
        client.Id.Should().Be("full-client");
        client.Url.Should().Be("https://full.example.com");
        client.Title.Should().Be("Full App");
        client.Description.Should().Be("A fully populated client");
        client.LogoUrl.Should().Be("https://example.com/logo.png");
        client.AutoAccess.Should().BeFalse();
    }
}
