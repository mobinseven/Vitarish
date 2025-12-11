using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Handlers;
using Vitarish.ApiClients.Tests.TestHelpers;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="DeleteApiClientHandler"/>.
/// </summary>
public sealed class DeleteApiClientHandlerTests : ApiClientDbContextTestBase
{

    [Fact]
    public async Task Handle_WithExistingClient_DeletesClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = "test-client",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new DeleteApiClient(Id: "test-client");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deleted = await db.ApiClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_DoesNotThrow()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteApiClientHandler(db, cache);

        var command = new DeleteApiClient(Id: "non-existing-client");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WithMultipleClients_DeletesOnlySpecifiedClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteApiClientHandler(db, cache);

        var clients = new[]
        {
            new ApiClient { Id = "client-1", Secret = "secret-1", Scopes = "scopes-1", HostUri = "https://example1.com" },
            new ApiClient { Id = "client-2", Secret = "secret-2", Scopes = "scopes-2", HostUri = "https://example2.com" },
            new ApiClient { Id = "client-3", Secret = "secret-3", Scopes = "scopes-3", HostUri = "https://example3.com" }
        };
        db.ApiClients.AddRange(clients);
        await db.SaveChangesAsync();

        var command = new DeleteApiClient(Id: "client-2");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remainingClients = await db.ApiClients.ToListAsync();
        remainingClients.Should().HaveCount(2);
        remainingClients.Select(c => c.Id).Should().Contain("client-1", "client-3");
        remainingClients.Select(c => c.Id).Should().NotContain("client-2");
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteApiClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new DeleteApiClient(Id: "test-client");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client.with.dots")]
    public async Task Handle_WithVariousIdFormats_DeletesCorrectClient(string id)
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new DeleteApiClientHandler(db, cache);

        var existingClient = new ApiClient
        {
            Id = id,
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };
        db.ApiClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new DeleteApiClient(Id: id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deleted = await db.ApiClients.FirstOrDefaultAsync(c => c.Id == id);
        deleted.Should().BeNull();
    }
}
