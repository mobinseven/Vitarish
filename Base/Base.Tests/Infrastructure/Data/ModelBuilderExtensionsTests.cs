using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Primitives;
using Vitarish.Base.Infrastructure.Data;

namespace Base.Tests.Infrastructure.Data;

public class ModelBuilderExtensionsTests
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
        private readonly bool _applySoftDelete;
        private readonly bool _configureDateTimeOffset;

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        public TestDbContext(
            DbContextOptions<TestDbContext> options,
            bool applySoftDelete = false,
            bool configureDateTimeOffset = false)
            : base(options)
        {
            _applySoftDelete = applySoftDelete;
            _configureDateTimeOffset = configureDateTimeOffset;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<TestEntity>().Ignore(e => e.DomainEvents);

            if (_applySoftDelete)
            {
                modelBuilder.ApplySoftDeleteQueryFilter();
            }

            if (_configureDateTimeOffset)
            {
                modelBuilder.ConfigureDateTimeOffsetConversions();
            }
        }
    }

    private static TestDbContext CreateContext(bool applySoftDelete = false, bool configureDateTimeOffset = false)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options, applySoftDelete, configureDateTimeOffset);
    }

    [Fact]
    public async Task ApplySoftDeleteQueryFilter_ExcludesDeletedEntities()
    {
        // Arrange
        await using var context = CreateContext(applySoftDelete: true);

        var entity1 = new TestEntity(Guid.NewGuid(), "Active");
        var entity2 = new TestEntity(Guid.NewGuid(), "Deleted");
        entity2.MarkAsDeleted();

        context.TestEntities.Add(entity1);
        context.TestEntities.Add(entity2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.TestEntities.ToListAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Active");
    }

    [Fact]
    public async Task ApplySoftDeleteQueryFilter_IncludesNonDeletedEntities()
    {
        // Arrange
        await using var context = CreateContext(applySoftDelete: true);

        var entity1 = new TestEntity(Guid.NewGuid(), "Entity1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Entity2");

        context.TestEntities.Add(entity1);
        context.TestEntities.Add(entity2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.TestEntities.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplySoftDeleteQueryFilter_CanIgnoreFilter()
    {
        // Arrange
        await using var context = CreateContext(applySoftDelete: true);

        var entity1 = new TestEntity(Guid.NewGuid(), "Active");
        var entity2 = new TestEntity(Guid.NewGuid(), "Deleted");
        entity2.MarkAsDeleted();

        context.TestEntities.Add(entity1);
        context.TestEntities.Add(entity2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.TestEntities.IgnoreQueryFilters().ToListAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConfigureDateTimeOffsetConversions_ConvertsDateTimeOffset()
    {
        // Arrange
        await using var context = CreateContext(configureDateTimeOffset: true);

        var entity = new TestEntity(Guid.NewGuid(), "Test");
        var originalCreatedAt = entity.CreatedAt;

        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await context.TestEntities.FirstAsync();

        // Assert
        result.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromSeconds(1));
    }
}
