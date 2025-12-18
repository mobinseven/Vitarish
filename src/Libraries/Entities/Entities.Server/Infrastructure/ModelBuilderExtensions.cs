using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Vitarish.Entities.Server.Infrastructure;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ConfigureEntityDateTimeOffsetConversions(this ModelBuilder modelBuilder)
    {
        ValueConverter<DateTimeOffset, DateTime> dtoToUtc = new(
            v => v.UtcDateTime,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        ValueConverter<DateTimeOffset?, DateTime?> dtoNullableToUtc = new(
            v => v.HasValue ? v.Value.UtcDateTime : null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null);

        foreach (IMutableEntityType? entityType in modelBuilder.Model.GetEntityTypes().Where(IsEntityType))
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                ConfigureProperty(property, dtoToUtc, dtoNullableToUtc);
            }
        }

        return modelBuilder;
    }

    public static ModelBuilder ConfigureEntitySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType? entityType in modelBuilder.Model.GetEntityTypes().Where(IsEntityType))
        {
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression deletedAtProperty = Expression.Property(parameter, nameof(Entity.DeletedAt));
            BinaryExpression notDeletedExpression = Expression.Equal(
                deletedAtProperty,
                Expression.Constant(null, typeof(DateTimeOffset?)));

            entityType.SetQueryFilter(
                EntityQueryFilterNames.SoftDelete,
                Expression.Lambda(notDeletedExpression, parameter));
        }

        return modelBuilder;
    }

    private static bool IsEntityType(IMutableEntityType entityType) => typeof(Entity).IsAssignableFrom(entityType.ClrType);

    private static void ConfigureProperty(
        IMutableProperty property,
        ValueConverter<DateTimeOffset, DateTime> dtoToUtc,
        ValueConverter<DateTimeOffset?, DateTime?> dtoNullableToUtc)
    {
        if (property.GetValueConverter() is not null)
        {
            return;
        }

        if (property.ClrType == typeof(DateTimeOffset))
        {
            property.SetValueConverter(dtoToUtc);
        }
        else if (property.ClrType == typeof(DateTimeOffset?))
        {
            property.SetValueConverter(dtoNullableToUtc);
        }
    }

}
