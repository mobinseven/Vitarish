using Vitarish.ApiClients.Domain.Events;

namespace Vitarish.ApiClients.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="ApiClientCreatedEvent"/> domain event.
/// </summary>
public sealed class ApiClientCreatedEventTests
{
    [Fact]
    public void ApiClientCreatedEvent_StoresClientId()
    {
        // Arrange
        const string clientId = "test-client-id";

        // Act
        var @event = new ApiClientCreatedEvent(clientId);

        // Assert
        @event.ClientId.Should().Be(clientId);
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("")]
    public void ApiClientCreatedEvent_AcceptsVariousClientIdFormats(string clientId)
    {
        // Arrange & Act
        var @event = new ApiClientCreatedEvent(clientId);

        // Assert
        @event.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void ApiClientCreatedEvent_ImplementsINotification()
    {
        // Arrange
        var @event = new ApiClientCreatedEvent("test-id");

        // Assert
        @event.Should().BeAssignableTo<MediatR.INotification>();
    }

    [Fact]
    public void ApiClientCreatedEvent_IsRecord_SupportsEquality()
    {
        // Arrange
        var event1 = new ApiClientCreatedEvent("test-id");
        var event2 = new ApiClientCreatedEvent("test-id");
        var event3 = new ApiClientCreatedEvent("different-id");

        // Assert
        event1.Should().Be(event2);
        event1.Should().NotBe(event3);
    }

    [Fact]
    public void ApiClientCreatedEvent_IsRecord_SupportsDeconstruction()
    {
        // Arrange
        var @event = new ApiClientCreatedEvent("test-id");

        // Act
        var clientId = @event.ClientId;

        // Assert
        clientId.Should().Be("test-id");
    }
}
