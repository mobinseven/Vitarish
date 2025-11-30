using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class AggregateRootTests
{
    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        public TestAggregate() { }

        public TestAggregate(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public void UpdateName(string name)
        {
            Name = name;
            IncrementVersion();
        }
    }

    [Fact]
    public void AggregateRoot_WhenCreated_HasVersionZero()
    {
        // Arrange & Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.Version.Should().Be(0u);
    }

    [Fact]
    public void AggregateRoot_WhenVersionIncremented_IncreasesVersion()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        aggregate.UpdateName("New Name");

        // Assert
        aggregate.Version.Should().Be(1u);
    }

    [Fact]
    public void AggregateRoot_WhenVersionIncrementedMultipleTimes_AccumulatesVersion()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");

        // Act
        aggregate.UpdateName("Name 1");
        aggregate.UpdateName("Name 2");
        aggregate.UpdateName("Name 3");

        // Assert
        aggregate.Version.Should().Be(3u);
    }

    [Fact]
    public void AggregateRoot_InheritsEntityBehavior()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate(id, "Test");

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }
}
