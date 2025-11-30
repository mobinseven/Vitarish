using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Primitives;
using Vitarish.Base.Infrastructure.Repositories;

namespace Base.Tests.Infrastructure.Repositories;

public class RepositoryBaseTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity() { }

        public TestEntity(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private sealed class TestDbContext : DbContext
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<TestEntity>().Ignore(e => e.DomainEvents);
        }
    }

    private sealed class TestRepository : RepositoryBase<TestEntity, Guid>
    {
        public TestRepository(DbContext context) : base(context) { }
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new TestRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        await using var context = CreateContext();
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Entity1"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Entity2"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Entity3"));
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        // Arrange
        await using var context = CreateContext();
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Alpha"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Beta"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Alpha"));
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.FindAsync(e => e.Name == "Alpha");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Name == "Alpha");
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityExists_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Test"));
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.ExistsAsync(e => e.Name == "Test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new TestRepository(context);

        // Act
        var result = await repository.ExistsAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_WithoutPredicate_ReturnsAllCount()
    {
        // Arrange
        await using var context = CreateContext();
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Entity1"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Entity2"));
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsFilteredCount()
    {
        // Arrange
        await using var context = CreateContext();
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Alpha"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Beta"));
        context.TestEntities.Add(new TestEntity(Guid.NewGuid(), "Alpha"));
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        var result = await repository.CountAsync(e => e.Name == "Alpha");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task Add_AddsEntityToRepository()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new TestRepository(context);
        var entity = new TestEntity(Guid.NewGuid(), "New Entity");

        // Act
        repository.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        context.TestEntities.Should().ContainSingle();
    }

    [Fact]
    public async Task AddRange_AddsMultipleEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new TestRepository(context);
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "Entity1"),
            new TestEntity(Guid.NewGuid(), "Entity2")
        };

        // Act
        repository.AddRange(entities);
        await context.SaveChangesAsync();

        // Assert
        context.TestEntities.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_UpdatesEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity(Guid.NewGuid(), "Original");
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        entity.Name = "Updated";
        repository.Update(entity);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.TestEntities.FindAsync(entity.Id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Remove_RemovesEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity(Guid.NewGuid(), "ToRemove");
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        repository.Remove(entity);
        await context.SaveChangesAsync();

        // Assert
        context.TestEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveRange_RemovesMultipleEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity(Guid.NewGuid(), "Entity1"),
            new TestEntity(Guid.NewGuid(), "Entity2")
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new TestRepository(context);

        // Act
        repository.RemoveRange(entities);
        await context.SaveChangesAsync();

        // Assert
        context.TestEntities.Should().BeEmpty();
    }
}
