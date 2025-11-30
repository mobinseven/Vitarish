using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class EnumerationTests
{
    private sealed class Status : Enumeration<Status>
    {
        public static readonly Status Pending = new(1, nameof(Pending));
        public static readonly Status Active = new(2, nameof(Active));
        public static readonly Status Completed = new(3, nameof(Completed));
        public static readonly Status Cancelled = new(4, nameof(Cancelled));

        private Status(int value, string name) : base(value, name) { }
    }

    [Fact]
    public void Enumeration_GetAll_ReturnsAllValues()
    {
        // Act
        var all = Status.GetAll();

        // Assert
        all.Should().HaveCount(4);
        all.Should().Contain(Status.Pending);
        all.Should().Contain(Status.Active);
        all.Should().Contain(Status.Completed);
        all.Should().Contain(Status.Cancelled);
    }

    [Fact]
    public void Enumeration_FromValue_ReturnsCorrectEnumeration()
    {
        // Act
        var status = Status.FromValue(2);

        // Assert
        status.Should().Be(Status.Active);
    }

    [Fact]
    public void Enumeration_FromValue_WithInvalidValue_ReturnsNull()
    {
        // Act
        var status = Status.FromValue(999);

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void Enumeration_FromName_ReturnsCorrectEnumeration()
    {
        // Act
        var status = Status.FromName("Active");

        // Assert
        status.Should().Be(Status.Active);
    }

    [Fact]
    public void Enumeration_FromName_IsCaseInsensitive()
    {
        // Act
        var status = Status.FromName("ACTIVE");

        // Assert
        status.Should().Be(Status.Active);
    }

    [Fact]
    public void Enumeration_FromName_WithInvalidName_ReturnsNull()
    {
        // Act
        var status = Status.FromName("Invalid");

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void Enumeration_TryFromValue_WithValidValue_ReturnsTrue()
    {
        // Act
        var result = Status.TryFromValue(1, out var status);

        // Assert
        result.Should().BeTrue();
        status.Should().Be(Status.Pending);
    }

    [Fact]
    public void Enumeration_TryFromValue_WithInvalidValue_ReturnsFalse()
    {
        // Act
        var result = Status.TryFromValue(999, out var status);

        // Assert
        result.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact]
    public void Enumeration_TryFromName_WithValidName_ReturnsTrue()
    {
        // Act
        var result = Status.TryFromName("Completed", out var status);

        // Assert
        result.Should().BeTrue();
        status.Should().Be(Status.Completed);
    }

    [Fact]
    public void Enumeration_TryFromName_WithInvalidName_ReturnsFalse()
    {
        // Act
        var result = Status.TryFromName("Invalid", out var status);

        // Assert
        result.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact]
    public void Enumeration_SameValue_AreEqual()
    {
        // Arrange
        var status1 = Status.Active;
        var status2 = Status.FromValue(2);

        // Assert
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
        status1.GetHashCode().Should().Be(status2!.GetHashCode());
    }

    [Fact]
    public void Enumeration_DifferentValue_AreNotEqual()
    {
        // Assert
        (Status.Active == Status.Pending).Should().BeFalse();
        (Status.Active != Status.Pending).Should().BeTrue();
    }

    [Fact]
    public void Enumeration_ToString_ReturnsName()
    {
        // Assert
        Status.Active.ToString().Should().Be("Active");
    }

    [Fact]
    public void Enumeration_ComparedToNull_ReturnsNotEqual()
    {
        // Assert
        Status.Active.Equals(null).Should().BeFalse();
        (Status.Active == null).Should().BeFalse();
        (Status.Active != null).Should().BeTrue();
    }
}
