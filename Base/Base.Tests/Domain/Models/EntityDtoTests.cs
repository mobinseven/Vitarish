namespace Vitarish.Base.Tests.Domain.Models;

using Vitarish.Base.Domain.Models;

public class EntityDtoTests
{
    [Fact]
    public void EntityDto_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var dto = new EntityDto();

        // Assert
        Assert.Equal(Guid.Empty, dto.Id);
        Assert.Equal(default, dto.CreatedAt);
        Assert.Null(dto.ModifiedAt);
    }

    [Fact]
    public void EntityDto_SetId_IdIsSet()
    {
        // Arrange
        var dto = new EntityDto();
        var id = Guid.NewGuid();

        // Act
        dto = dto with { Id = id };

        // Assert
        Assert.Equal(id, dto.Id);
    }

    [Fact]
    public void EntityDto_SetCreatedAt_CreatedAtIsSet()
    {
        // Arrange
        var dto = new EntityDto();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        dto = dto with { CreatedAt = createdAt };

        // Assert
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    [Fact]
    public void EntityDto_SetModifiedAt_ModifiedAtIsSet()
    {
        // Arrange
        var dto = new EntityDto();
        var modifiedAt = DateTimeOffset.UtcNow;

        // Act
        dto = dto with { ModifiedAt = modifiedAt };

        // Assert
        Assert.Equal(modifiedAt, dto.ModifiedAt);
    }
}

public class EntityDescriptorTests
{
    [Fact]
    public void EntityDescriptor_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var descriptor = new EntityDescriptor();

        // Assert
        Assert.Equal(Guid.Empty, descriptor.Id);
    }

    [Fact]
    public void EntityDescriptor_SetId_IdIsSet()
    {
        // Arrange
        var descriptor = new EntityDescriptor();
        var id = Guid.NewGuid();

        // Act
        descriptor = descriptor with { Id = id };

        // Assert
        Assert.Equal(id, descriptor.Id);
    }
}

public class EntityDescriptorDtoTests
{
    [Fact]
    public void EntityDescriptorDto_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var descriptor = new EntityDescriptorDto();

        // Assert
        Assert.Equal(Guid.Empty, descriptor.Id);
    }

    [Fact]
    public void EntityDescriptorDto_SetId_IdIsSet()
    {
        // Arrange
        var descriptor = new EntityDescriptorDto();
        var id = Guid.NewGuid();

        // Act
        descriptor = descriptor with { Id = id };

        // Assert
        Assert.Equal(id, descriptor.Id);
    }
}
