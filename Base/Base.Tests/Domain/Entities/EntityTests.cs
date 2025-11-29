namespace Vitarish.Base.Tests.Domain.Entities;

using Vitarish.Base.Domain.Entities;

public class EntityTests
{
    [Fact]
    public void Entity_NewInstance_HasGeneratedId()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Entity_NewInstance_HasCreatedAtSet()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        var entity = new TestEntity();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(entity.CreatedAt >= before);
        Assert.True(entity.CreatedAt <= after);
    }

    [Fact]
    public void Entity_NewInstance_ModifiedAtIsNull()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Null(entity.ModifiedAt);
    }

    [Fact]
    public void Entity_NewInstance_IsDeletedIsFalse()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void Entity_SetId_IdIsSet()
    {
        // Arrange
        var entity = new TestEntity();
        var newId = Guid.NewGuid();

        // Act
        entity.Id = newId;

        // Assert
        Assert.Equal(newId, entity.Id);
    }

    [Fact]
    public void Entity_SetModifiedAt_ModifiedAtIsSet()
    {
        // Arrange
        var entity = new TestEntity();
        var modifiedAt = DateTimeOffset.UtcNow;

        // Act
        entity.ModifiedAt = modifiedAt;

        // Assert
        Assert.Equal(modifiedAt, entity.ModifiedAt);
    }

    [Fact]
    public void Entity_SetIsDeleted_IsDeletedIsSet()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.IsDeleted = true;

        // Assert
        Assert.True(entity.IsDeleted);
    }

    private class TestEntity : Entity { }
}
