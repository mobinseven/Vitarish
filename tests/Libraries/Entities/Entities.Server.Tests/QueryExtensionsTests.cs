using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using Vitarish.Entities.Server.Infrastructure;

namespace Entities.Server.Tests;

public class QueryExtensionsTests
{
    [Fact]
    public void ConfigureEntitySoftDeleteFilters_RegistersNamedFilter()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using TestDbContext context = new(options);

        IQueryFilter? filter = context.Model.FindEntityType(typeof(SoftDeleteEntity))!
            .FindDeclaredQueryFilter(EntityQueryFilterNames.SoftDelete);

        Assert.NotNull(filter);

        Assert.Equal(EntityQueryFilterNames.SoftDelete, filter!.Key);

        LambdaExpression? predicateExpression = filter.Expression;

        Assert.NotNull(predicateExpression);

        Func<SoftDeleteEntity, bool> predicate = (Func<SoftDeleteEntity, bool>)predicateExpression!.Compile();

        Assert.True(predicate(new SoftDeleteEntity { DeletedAt = null }));
        Assert.False(predicate(new SoftDeleteEntity { DeletedAt = DateTimeOffset.UtcNow }));
    }

    [Fact]
    public async Task ApplyDescription_IgnoresOnlySoftDeleteFilterWhenDeletedCriteriaPresent()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using TestDbContext context = new(options);

        SoftDeleteEntity visible = new() { Name = "Visible" };
        SoftDeleteEntity deleted = new() { Name = "Deleted", DeletedAt = DateTimeOffset.UtcNow };
        SoftDeleteEntity hidden = new() { Name = "Hidden", IsHidden = true };
        SoftDeleteEntity hiddenDeleted = new() { Name = "HiddenDeleted", IsHidden = true, DeletedAt = DateTimeOffset.UtcNow };

        context.Entities.AddRange(visible, deleted, hidden, hiddenDeleted);
        _ = await context.SaveChangesAsync();

        EntityDescriptor descriptor = new() { DeletedFrom = DateTimeOffset.MinValue };

        List<SoftDeleteEntity> result = await context.Entities
            .ApplyDescription(descriptor)
            .OrderBy(entity => entity.Name)
            .ToListAsync();

        Assert.Equal(new[] { deleted.Name }, result.Select(entity => entity.Name));
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<SoftDeleteEntity> Entities => Set<SoftDeleteEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ConfigureEntitySoftDeleteFilters();

            _ = modelBuilder.Entity<SoftDeleteEntity>()
                .HasQueryFilter("Hidden", entity => !entity.IsHidden);
        }
    }

    private sealed class SoftDeleteEntity : Entity
    {
        public string Name { get; set; } = string.Empty;

        public bool IsHidden { get; set; }
    }
}
