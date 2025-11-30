using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Internal;
using Xunit;

namespace Vorn.Aaas.Client.Api.Tests;

public class RequestIdentityForwarderTests
{
    [Fact]
    public async Task ForwardAsync_UpdatesContext_WhenResponseIsSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var mockClientFactory = new Mock<IAaasClientFactory>();
        mockClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(httpClient);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockClientFactory.Object);
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<RequestIdentityForwarder>>();
        var forwarder = new RequestIdentityForwarder(serviceProvider, logger);

        var mockWriter = new Mock<IRequestIdentityContextWriter>();
        var envelope = new RequestIdentityEnvelope(
            "test-request-id",
            "https://authority.com",
            "test-client-id",
            "test-owner-id",
            "test-tenant-id",
            "test-user-id",
            "/api/resource",
            "trace-123",
            null,
            DateTimeOffset.UtcNow);

        // Act
        await forwarder.ForwardAsync(envelope, mockWriter.Object);

        // Assert
        mockWriter.Verify(w => w.Write(It.Is<RequestIdentityContextCommand>(cmd =>
            cmd.TenantId == "test-tenant-id" &&
            cmd.OwnerId == "test-owner-id" &&
            cmd.Authority == "https://authority.com" &&
            cmd.ClientId == "test-client-id" &&
            cmd.RequestId == "test-request-id"
        )), Times.Once);
    }

    [Fact]
    public async Task ForwardAsync_DoesNotUpdateContext_WhenResponseIsNotSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var mockClientFactory = new Mock<IAaasClientFactory>();
        mockClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(httpClient);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockClientFactory.Object);
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<RequestIdentityForwarder>>();
        var forwarder = new RequestIdentityForwarder(serviceProvider, logger);

        var mockWriter = new Mock<IRequestIdentityContextWriter>();
        var envelope = new RequestIdentityEnvelope(
            "test-request-id",
            "https://authority.com",
            "test-client-id",
            "test-owner-id",
            "test-tenant-id",
            "test-user-id",
            "/api/resource",
            "trace-123",
            null,
            DateTimeOffset.UtcNow);

        // Act
        await forwarder.ForwardAsync(envelope, mockWriter.Object);

        // Assert
        mockWriter.Verify(w => w.Write(It.IsAny<RequestIdentityContextCommand>()), Times.Never);
    }

    [Fact]
    public async Task ForwardAsync_DoesNotUpdateContext_WhenResponseIsConflict()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Conflict
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var mockClientFactory = new Mock<IAaasClientFactory>();
        mockClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(httpClient);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockClientFactory.Object);
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<RequestIdentityForwarder>>();
        var forwarder = new RequestIdentityForwarder(serviceProvider, logger);

        var mockWriter = new Mock<IRequestIdentityContextWriter>();
        var envelope = new RequestIdentityEnvelope(
            "test-request-id",
            "https://authority.com",
            "test-client-id",
            "test-owner-id",
            "test-tenant-id",
            "test-user-id",
            "/api/resource",
            "trace-123",
            null,
            DateTimeOffset.UtcNow);

        // Act
        await forwarder.ForwardAsync(envelope, mockWriter.Object);

        // Assert
        mockWriter.Verify(w => w.Write(It.IsAny<RequestIdentityContextCommand>()), Times.Never);
    }

    [Fact]
    public async Task ForwardAsync_DoesNotUpdateContext_WhenWriterIsNull()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var mockClientFactory = new Mock<IAaasClientFactory>();
        mockClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(httpClient);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockClientFactory.Object);
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<RequestIdentityForwarder>>();
        var forwarder = new RequestIdentityForwarder(serviceProvider, logger);

        var envelope = new RequestIdentityEnvelope(
            "test-request-id",
            "https://authority.com",
            "test-client-id",
            "test-owner-id",
            "test-tenant-id",
            "test-user-id",
            "/api/resource",
            "trace-123",
            null,
            DateTimeOffset.UtcNow);

        // Act - no exception should be thrown
        await forwarder.ForwardAsync(envelope, writer: null);

        // Assert - just verify it completes without exception
        Assert.True(true);
    }

    [Fact]
    public async Task ForwardAsync_HandlesException_DoesNotThrow()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var mockClientFactory = new Mock<IAaasClientFactory>();
        mockClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(httpClient);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockClientFactory.Object);
        serviceCollection.AddLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<RequestIdentityForwarder>>();
        var forwarder = new RequestIdentityForwarder(serviceProvider, logger);

        var mockWriter = new Mock<IRequestIdentityContextWriter>();
        var envelope = new RequestIdentityEnvelope(
            "test-request-id",
            "https://authority.com",
            "test-client-id",
            "test-owner-id",
            "test-tenant-id",
            "test-user-id",
            "/api/resource",
            "trace-123",
            null,
            DateTimeOffset.UtcNow);

        // Act - should not throw
        await forwarder.ForwardAsync(envelope, mockWriter.Object);

        // Assert
        mockWriter.Verify(w => w.Write(It.IsAny<RequestIdentityContextCommand>()), Times.Never);
    }
}
