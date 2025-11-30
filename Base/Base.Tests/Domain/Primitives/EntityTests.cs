using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity() { }

        public TestEntity(Guid id)
        {
            Id = id;
        }

        public void SetName(string name)
        {
            Name = name;
            MarkAsUpdated();
        }

        public void RaiseEvent(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }

    private sealed record TestDomainEvent : DomainEventBase;

    [Fact]
    public void Entity_WhenCreated_HasDefaultCreatedAt()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Entity_WhenCreated_HasNullUpdatedAt()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Entity_WhenCreated_HasNullDeletedAt()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Entity_WhenCreated_HasEmptyDomainEvents()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Entity_WhenDomainEventRaised_AddsToCollection()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        // Act
        entity.RaiseEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void Entity_WhenDomainEventsCleared_CollectionIsEmpty()
    {
        // Arrange
        var entity = new TestEntity();
        entity.RaiseEvent(new TestDomainEvent());
        entity.RaiseEvent(new TestDomainEvent());

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Entity_WhenMarkedAsDeleted_SetsDeletedAt()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.MarkAsDeleted();

        // Assert
        entity.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Entity_WhenMarkedAsUpdated_SetsUpdatedAt()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SetName("Test");

        // Assert
        entity.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Entity_WithSameId_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Assert
        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Entity_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Assert
        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void Entity_ComparedToNull_ReturnsNotEqual()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (entity != null).Should().BeTrue();
    }

    [Fact]
    public void Entity_ComparedToSelf_ReturnsEqual()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Assert
        entity.Equals(entity).Should().BeTrue();
#pragma warning disable CS1718
        (entity == entity).Should().BeTrue();
#pragma warning restore CS1718
    }
}
