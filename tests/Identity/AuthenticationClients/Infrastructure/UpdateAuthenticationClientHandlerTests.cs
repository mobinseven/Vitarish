using Microsoft.EntityFrameworkCore;
using Vitarish.AuthenticationClients.Infrastructure.Handlers;
using Vitarish.AuthenticationClients.Tests.TestHelpers;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Integration tests for the <see cref="UpdateAuthenticationClientHandler"/>.
/// </summary>
public sealed class UpdateAuthenticationClientHandlerTests : AuthenticationClientDbContextTestBase
{
    [Fact]
    public async Task Handle_WithExistingClient_UpdatesClient()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateAuthenticationClientHandler(db, cache);

        // Create existing client
        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://original.example.com",
            Title = "Original Title",
            Description = "Original Description",
            LogoUrl = "https://example.com/original-logo.png",
            AutoAccess = true
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateAuthenticationClient(
            Id: "test-client",
            Url: "https://updated.example.com",
            Title: "Updated Title",
            Description: "Updated Description",
            LogoUrl: "https://example.com/updated-logo.png",
            AutoAccess: false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Use new context to verify persistence
        await using var assertDb = CreateInMemoryDbContext();
        var updated = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        updated.Should().NotBeNull();
        updated!.Url.Should().Be("https://updated.example.com");
        updated.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated Description");
        updated.LogoUrl.Should().Be("https://example.com/updated-logo.png");
        updated.AutoAccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistingClient_DoesNothing()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateAuthenticationClientHandler(db, cache);

        var command = new UpdateAuthenticationClient(
            Id: "non-existing-client",
            Url: "https://updated.example.com",
            Title: "Updated Title",
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act - Should not throw
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var assertDb = CreateInMemoryDbContext();
        var client = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "non-existing-client");
        client.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateAuthenticationClientHandler(db, cache);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new UpdateAuthenticationClient(
            Id: "test-client",
            Url: "https://updated.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_CanSetOptionalFieldsToNull()
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateAuthenticationClientHandler(db, cache);

        // Create existing client with all fields populated
        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://original.example.com",
            Title = "Original Title",
            Description = "Original Description",
            LogoUrl = "https://example.com/logo.png",
            AutoAccess = true
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateAuthenticationClient(
            Id: "test-client",
            Url: "https://updated.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: false);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var assertDb = CreateInMemoryDbContext();
        var updated = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        updated.Should().NotBeNull();
        updated!.Title.Should().BeNull();
        updated.Description.Should().BeNull();
        updated.LogoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("https://app1.example.com")]
    [InlineData("https://app2.example.com:8080")]
    [InlineData("http://localhost:3000")]
    public async Task Handle_WithVariousUrlFormats_UpdatesCorrectly(string newUrl)
    {
        // Arrange
        await using var db = CreateInMemoryDbContext();
        var cache = new FakeDistributedCache();
        var handler = new UpdateAuthenticationClientHandler(db, cache);

        var existingClient = new AuthenticationClient
        {
            Id = "test-client",
            Url = "https://original.example.com"
        };
        db.AuthenticationClients.Add(existingClient);
        await db.SaveChangesAsync();

        var command = new UpdateAuthenticationClient(
            Id: "test-client",
            Url: newUrl,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var assertDb = CreateInMemoryDbContext();
        var updated = await assertDb.AuthenticationClients.FirstOrDefaultAsync(c => c.Id == "test-client");
        updated.Should().NotBeNull();
        updated!.Url.Should().Be(newUrl);
    }
}
