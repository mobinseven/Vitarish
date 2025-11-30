using Vitarish.Identity.Core.Domain.Events;

namespace Identity.Core.Tests.Domain.Events;

public class UserAccessClaimSetEventTests
{
    [Fact]
    public void UserAccessClaimSetEvent_CreatedWithCorrectData()
    {
        // Arrange
        var userId = "user-123";
        var clientId = "client-456";
        var accessValue = "admin";

        // Act
        var domainEvent = new UserAccessClaimSetEvent(userId, clientId, accessValue);

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ClientId.Should().Be(clientId);
        domainEvent.AccessValue.Should().Be(accessValue);
    }

    [Fact]
    public void UserAccessClaimSetEvent_HasEventId()
    {
        // Arrange & Act
        var domainEvent = new UserAccessClaimSetEvent("user", "client", "value");

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UserAccessClaimSetEvent_HasOccurredAt()
    {
        // Arrange & Act
        var domainEvent = new UserAccessClaimSetEvent("user", "client", "value");

        // Assert
        domainEvent.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserAccessClaimSetEvent_TwoInstances_HaveDifferentEventIds()
    {
        // Arrange & Act
        var event1 = new UserAccessClaimSetEvent("user", "client", "value");
        var event2 = new UserAccessClaimSetEvent("user", "client", "value");

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }
}
