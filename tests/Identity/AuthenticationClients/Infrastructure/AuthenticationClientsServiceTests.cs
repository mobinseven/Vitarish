using MediatR;
using Vitarish.AuthenticationClients.Infrastructure.Services;

namespace Vitarish.AuthenticationClients.Tests.Infrastructure;

/// <summary>
/// Unit tests for the <see cref="AuthenticationClientsService"/>.
/// </summary>
public sealed class AuthenticationClientsServiceTests
{
    [Fact]
    public async Task GetAuthenticationClientAsync_SendsCorrectQuery()
    {
        // Arrange
        var expectedClient = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com",
            Title = "Test App"
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.Is<GetAuthenticationClient>(q => q.Id == "test-id"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClient);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        var result = await service.GetAuthenticationClientAsync("test-id");

        // Assert
        result.Should().Be(expectedClient);
        mockSender.Verify(s => s.Send(It.Is<GetAuthenticationClient>(q => q.Id == "test-id"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationClientsAsync_SendsCorrectQuery()
    {
        // Arrange
        var expectedClients = new[]
        {
            new AuthenticationClient { Id = "client-1", Url = "https://example1.com" },
            new AuthenticationClient { Id = "client-2", Url = "https://example2.com" }
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetAuthenticationClients>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClients);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        var result = await service.GetAuthenticationClientsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedClients);
        mockSender.Verify(s => s.Send(It.IsAny<GetAuthenticationClients>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAuthenticationClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "test-id",
            Url: "https://example.com",
            Title: "Test App",
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        var expectedClient = new AuthenticationClient
        {
            Id = "test-id",
            Url = "https://example.com",
            Title = "Test App"
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClient);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        var result = await service.CreateAuthenticationClientAsync(command);

        // Assert
        result.Should().Be(expectedClient);
        mockSender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuthenticationClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var command = new UpdateAuthenticationClient(
            Id: "test-id",
            Url: "https://updated.example.com",
            Title: "Updated App",
            Description: null,
            LogoUrl: null,
            AutoAccess: false);

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.UpdateAuthenticationClientAsync(command);

        // Assert
        mockSender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAuthenticationClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var command = new DeleteAuthenticationClient(Id: "test-id");

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.Is<DeleteAuthenticationClient>(c => c.Id == "test-id"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.DeleteAuthenticationClientAsync(command);

        // Assert
        mockSender.Verify(s => s.Send(It.Is<DeleteAuthenticationClient>(c => c.Id == "test-id"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetAuthenticationClient>(), cts.Token))
            .ReturnsAsync((AuthenticationClient?)null);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.GetAuthenticationClientAsync("test-id", cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<GetAuthenticationClient>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationClientsAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetAuthenticationClients>(), cts.Token))
            .ReturnsAsync(Array.Empty<AuthenticationClient>());

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.GetAuthenticationClientsAsync(cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<GetAuthenticationClients>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateAuthenticationClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new CreateAuthenticationClient("id", "https://example.com", null, null, null, true);
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, cts.Token))
            .ReturnsAsync((AuthenticationClient?)null);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.CreateAuthenticationClientAsync(command, cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(command, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateAuthenticationClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new UpdateAuthenticationClient("id", "https://example.com", null, null, null, true);
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, cts.Token))
            .Returns(Task.CompletedTask);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.UpdateAuthenticationClientAsync(command, cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(command, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAuthenticationClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new DeleteAuthenticationClient("test-id");
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<DeleteAuthenticationClient>(), cts.Token))
            .Returns(Task.CompletedTask);

        var service = new AuthenticationClientsService(mockSender.Object);

        // Act
        await service.DeleteAuthenticationClientAsync(command, cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<DeleteAuthenticationClient>(), cts.Token), Times.Once);
    }
}
