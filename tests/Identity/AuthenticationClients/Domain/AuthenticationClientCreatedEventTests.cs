using Vitarish.AuthenticationClients.Domain.Events;

namespace Vitarish.AuthenticationClients.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="AuthenticationClientCreatedEvent"/> domain event.
/// </summary>
public sealed class AuthenticationClientCreatedEventTests
{
    [Fact]
    public void AuthenticationClientCreatedEvent_StoresClientId()
    {
        // Arrange
        const string clientId = "test-client-id";

        // Act
        var @event = new AuthenticationClientCreatedEvent(clientId);

        // Assert
        @event.ClientId.Should().Be(clientId);
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("")]
    public void AuthenticationClientCreatedEvent_AcceptsVariousClientIdFormats(string clientId)
    {
        // Arrange & Act
        var @event = new AuthenticationClientCreatedEvent(clientId);

        // Assert
        @event.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_ImplementsINotification()
    {
        // Arrange
        var @event = new AuthenticationClientCreatedEvent("test-id");

        // Assert
        @event.Should().BeAssignableTo<MediatR.INotification>();
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_IsRecord_SupportsEquality()
    {
        // Arrange
        var event1 = new AuthenticationClientCreatedEvent("test-id");
        var event2 = new AuthenticationClientCreatedEvent("test-id");
        var event3 = new AuthenticationClientCreatedEvent("different-id");

        // Assert
        event1.Should().Be(event2);
        event1.Should().NotBe(event3);
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_IsRecord_SupportsDeconstruction()
    {
        // Arrange
        var @event = new AuthenticationClientCreatedEvent("test-id");

        // Act
        var clientId = @event.ClientId;

        // Assert
        clientId.Should().Be("test-id");
    }
}
