using Vitarish.Identity.Core.Domain.Events;

namespace Identity.Core.Tests.Domain.Events;

public class UserAccessClaimRemovedEventTests
{
    [Fact]
    public void UserAccessClaimRemovedEvent_CreatedWithCorrectData()
    {
        // Arrange
        var userId = "user-789";
        var clientId = "client-012";

        // Act
        var domainEvent = new UserAccessClaimRemovedEvent(userId, clientId);

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void UserAccessClaimRemovedEvent_HasEventId()
    {
        // Arrange & Act
        var domainEvent = new UserAccessClaimRemovedEvent("user", "client");

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UserAccessClaimRemovedEvent_HasOccurredAt()
    {
        // Arrange & Act
        var domainEvent = new UserAccessClaimRemovedEvent("user", "client");

        // Assert
        domainEvent.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserAccessClaimRemovedEvent_TwoInstances_HaveDifferentEventIds()
    {
        // Arrange & Act
        var event1 = new UserAccessClaimRemovedEvent("user", "client");
        var event2 = new UserAccessClaimRemovedEvent("user", "client");

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }
}
