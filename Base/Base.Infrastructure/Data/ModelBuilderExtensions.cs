using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Base.Infrastructure.Data;

/// <summary>
/// Extension methods for configuring Entity Framework model builder with common conventions.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures DateTimeOffset to DateTime conversions for SQLite compatibility.
    /// </summary>
    public static ModelBuilder ConfigureDateTimeOffsetConversions(this ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
            v => v.UtcDateTime,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
            v => v.HasValue ? v.Value.UtcDateTime : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                }
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Applies soft delete query filter to all entities that have DeletedAt property.
    /// </summary>
    public static ModelBuilder ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var deletedAtProperty = entityType.FindProperty("DeletedAt");
            if (deletedAtProperty != null && deletedAtProperty.ClrType == typeof(DateTimeOffset?))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "DeletedAt");
                var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?));
                var equalExpression = System.Linq.Expressions.Expression.Equal(property, nullConstant);
                var lambda = System.Linq.Expressions.Expression.Lambda(equalExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        return modelBuilder;
    }
}

/// <summary>
/// Extension methods for configuring entity type builders.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures the entity as a base entity with standard properties.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureBaseEntity<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : Entity<TId>
        where TId : notnull
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.DeletedAt);

        builder.Ignore(e => e.DomainEvents);

        return builder;
    }

    /// <summary>
    /// Configures the entity to use soft delete with a query filter.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureSoftDelete<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : Entity<TId>
        where TId : notnull
    {
        builder.HasQueryFilter(e => e.DeletedAt == null);

        return builder;
    }
}
