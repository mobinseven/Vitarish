using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class StronglyTypedIdTests
{
    private sealed class UserId : GuidId
    {
        public UserId(Guid value) : base(value) { }

        public static UserId Create() => new(Guid.NewGuid());
    }

    private sealed class TenantCode : StringId
    {
        public TenantCode(string value) : base(value) { }
    }

    [Fact]
    public void GuidId_WithSameValue_AreEqual()
    {
        // Arrange
        var value = Guid.NewGuid();
        var id1 = new UserId(value);
        var id2 = new UserId(value);

        // Assert
        id1.Should().Be(id2);
        id1.Value.Should().Be(value);
    }

    [Fact]
    public void GuidId_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = UserId.Create();
        var id2 = UserId.Create();

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void GuidId_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        var act = () => new UserId(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void GuidId_ToString_ReturnsGuidString()
    {
        // Arrange
        var value = Guid.NewGuid();
        var id = new UserId(value);

        // Assert
        id.ToString().Should().Be(value.ToString());
    }

    [Fact]
    public void StringId_WithSameValue_AreEqual()
    {
        // Arrange
        var id1 = new TenantCode("TENANT-001");
        var id2 = new TenantCode("TENANT-001");

        // Assert
        id1.Should().Be(id2);
        id1.Value.Should().Be("TENANT-001");
    }

    [Fact]
    public void StringId_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = new TenantCode("TENANT-001");
        var id2 = new TenantCode("TENANT-002");

        // Assert
        id1.Should().NotBe(id2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StringId_WithInvalidValue_ThrowsArgumentException(string? value)
    {
        // Act
        var act = () => new TenantCode(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or whitespace*");
    }

    [Fact]
    public void StringId_ToString_ReturnsStringValue()
    {
        // Arrange
        var id = new TenantCode("TENANT-001");

        // Assert
        id.ToString().Should().Be("TENANT-001");
    }
}
