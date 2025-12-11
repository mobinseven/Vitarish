using MediatR;
using Vitarish.ApiClients.Infrastructure.Services;

namespace Vitarish.ApiClients.Tests.Infrastructure;

/// <summary>
/// Unit tests for the <see cref="ApiClientsService"/>.
/// </summary>
public sealed class ApiClientsServiceTests
{
    [Fact]
    public async Task GetApiClientAsync_SendsCorrectQuery()
    {
        // Arrange
        var expectedClient = new ApiClient
        {
            Id = "test-id",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.Is<GetApiClient>(q => q.Id == "test-id"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClient);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        var result = await service.GetApiClientAsync("test-id");

        // Assert
        result.Should().Be(expectedClient);
        mockSender.Verify(s => s.Send(It.Is<GetApiClient>(q => q.Id == "test-id"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetApiClientsAsync_SendsCorrectQuery()
    {
        // Arrange
        var expectedClients = new[]
        {
            new ApiClient { Id = "client-1", Secret = "secret-1", Scopes = "scopes-1", HostUri = "https://example1.com" },
            new ApiClient { Id = "client-2", Secret = "secret-2", Scopes = "scopes-2", HostUri = "https://example2.com" }
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetApiClients>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClients);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        var result = await service.GetApiClientsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedClients);
        mockSender.Verify(s => s.Send(It.IsAny<GetApiClients>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateApiClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "test-id",
            Secret: "secret",
            Scopes: "scopes",
            HostUri: "https://example.com");

        var expectedClient = new ApiClient
        {
            Id = "test-id",
            Secret = "secret",
            Scopes = "scopes",
            HostUri = "https://example.com"
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClient);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        var result = await service.CreateApiClientAsync(command);

        // Assert
        result.Should().Be(expectedClient);
        mockSender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateApiClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var command = new UpdateApiClient(
            Id: "test-id",
            Secret: "updated-secret",
            Scopes: "updated-scopes",
            HostUri: "https://updated.example.com");

        var expectedClient = new ApiClient
        {
            Id = "test-id",
            Secret = "updated-secret",
            Scopes = "updated-scopes",
            HostUri = "https://updated.example.com"
        };

        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClient);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        var result = await service.UpdateApiClientAsync(command);

        // Assert
        result.Should().Be(expectedClient);
        mockSender.Verify(s => s.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteApiClientAsync_SendsCorrectCommand()
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.Is<DeleteApiClient>(c => c.Id == "test-id"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.DeleteApiClientAsync("test-id");

        // Assert
        mockSender.Verify(s => s.Send(It.Is<DeleteApiClient>(c => c.Id == "test-id"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetApiClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetApiClient>(), cts.Token))
            .ReturnsAsync((ApiClient?)null);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.GetApiClientAsync("test-id", cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<GetApiClient>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetApiClientsAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<GetApiClients>(), cts.Token))
            .ReturnsAsync(Array.Empty<ApiClient>());

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.GetApiClientsAsync(cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<GetApiClients>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateApiClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new CreateApiClient("id", "secret", "scopes", "https://example.com");
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, cts.Token))
            .ReturnsAsync((ApiClient?)null);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.CreateApiClientAsync(command, cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(command, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateApiClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var command = new UpdateApiClient("id", "secret", "scopes", "https://example.com");
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(command, cts.Token))
            .ReturnsAsync((ApiClient?)null);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.UpdateApiClientAsync(command, cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(command, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteApiClientAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockSender = new Mock<ISender>();
        mockSender
            .Setup(s => s.Send(It.IsAny<DeleteApiClient>(), cts.Token))
            .Returns(Task.CompletedTask);

        var service = new ApiClientsService(mockSender.Object);

        // Act
        await service.DeleteApiClientAsync("test-id", cts.Token);

        // Assert
        mockSender.Verify(s => s.Send(It.IsAny<DeleteApiClient>(), cts.Token), Times.Once);
    }
}
