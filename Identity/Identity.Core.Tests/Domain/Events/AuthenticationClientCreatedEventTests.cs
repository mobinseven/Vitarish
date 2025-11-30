using Vitarish.Identity.Core.Domain.Events;

namespace Identity.Core.Tests.Domain.Events;

public class AuthenticationClientCreatedEventTests
{
    [Fact]
    public void AuthenticationClientCreatedEvent_CreatedWithCorrectData()
    {
        // Arrange
        var clientId = "new-client";

        // Act
        var domainEvent = new AuthenticationClientCreatedEvent(clientId);

        // Assert
        domainEvent.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_HasEventId()
    {
        // Arrange & Act
        var domainEvent = new AuthenticationClientCreatedEvent("client");

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_HasOccurredAt()
    {
        // Arrange & Act
        var domainEvent = new AuthenticationClientCreatedEvent("client");

        // Assert
        domainEvent.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AuthenticationClientCreatedEvent_TwoInstances_HaveDifferentEventIds()
    {
        // Arrange & Act
        var event1 = new AuthenticationClientCreatedEvent("client");
        var event2 = new AuthenticationClientCreatedEvent("client");

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }
}
