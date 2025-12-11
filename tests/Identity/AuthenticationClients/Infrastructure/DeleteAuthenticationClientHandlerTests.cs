using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Infrastructure.Handlers;
using Vitarish.AuthenticationClients.Tests.TestHelpers;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="DeleteAuthenticationClientHandler"/>.
/// </summary>
public sealed class DeleteAuthenticationClientHandlerTests : AuthenticationClientDbContextTestBase
{
    [Fact]
    public async Task Handle_WithExistingClient_DeletesClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteAuthenticationClientHandler(db, cache);

        // Create existing client
        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new DeleteAuthenticationClient(Id: "test-client");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Use new context to verify deletion
        await using var assertDb = CreateInMemoryDbContext();
        var deleted = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_DoesNothing()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteAuthenticationClientHandler(db, cache);

        var command = new DeleteAuthenticationClient(Id: "non-existing-client");

        // Act - Should not throw
        await handler.Handle(command, CancellationToken.None);

        // Assert - No exception and database unchanged
        await using var assertDb = CreateInMemoryDbContext();
        var count = await assertDb.AuthenticationClients.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteAuthenticationClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new DeleteAuthenticationClient(Id: "test-client");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_WithMultipleClients_OnlyDeletesSpecified()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteAuthenticationClientHandler(db, cache);

        // Create multiple clients
        db.AuthenticationClients.AddRange(
            new AuthenticationClient { Id = "client-1", Url = "https://example1.com" },
            new AuthenticationClient { Id = "client-2", Url = "https://example2.com" },
            new AuthenticationClient { Id = "client-3", Url = "https://example3.com" }
        );
        await db.SaveChangesAsync();

        var command = new DeleteAuthenticationClient(Id: "client-2");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var assertDb = CreateInMemoryDbContext();
        var remainingClients = await assertDb.AuthenticationClients.ToListAsync();
        remainingClients.Should().HaveCount(2);
        remainingClients.Should().NotContain(c => c.Id == "client-2");
        remainingClients.Should().Contain(c => c.Id == "client-1");
        remainingClients.Should().Contain(c => c.Id == "client-3");
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    public async Task Handle_WithVariousIdFormats_DeletesCorrectly(string id)
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteAuthenticationClientHandler(db, cache);

        var existingClient = new AuthenticationClient
        {
            Id = id,
            Url = "https://example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new DeleteAuthenticationClient(Id: id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var assertDb = CreateInMemoryDbContext();
        var deleted = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == id);
        deleted.Should().BeNull();
    }
}
