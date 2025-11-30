using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class DomainEventTests
{
    private sealed record TestEvent(string Data) : DomainEventBase;

    [Fact]
    public void DomainEventBase_WhenCreated_HasUniqueEventId()
    {
        // Arrange & Act
        var event1 = new TestEvent("Data1");
        var event2 = new TestEvent("Data2");

        // Assert
        event1.EventId.Should().NotBeEmpty();
        event2.EventId.Should().NotBeEmpty();
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEventBase_WhenCreated_HasOccurredAtSet()
    {
        // Arrange & Act
        var domainEvent = new TestEvent("Test");

        // Assert
        domainEvent.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DomainEventBase_ImplementsIDomainEvent()
    {
        // Arrange & Act
        var domainEvent = new TestEvent("Test");

        // Assert
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }
}
