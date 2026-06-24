using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Vitarish.Entities.Server.Infrastructure;

public static class ModelBuilderExtensions
{
    private const string PostgreSqlUtcInstantColumnType = "timestamp with time zone";

    private static readonly HashSet<string> UtcInstantPropertyNames =
    [
        nameof(Entity.CreatedAt),
        nameof(Entity.UpdatedAt),
        nameof(Entity.DeletedAt),
        "OccurredAt",
        "SubmittedAt",
        "PublishedAt",
        "ExpiresAt",
        "TokenExpiresAt",
        "LastAttemptAt",
        "NextRetryAt",
        "LastSyncedAt",
        "LastEligibilityCheckedAt",
        "LastTestedAt",
        "LastUsedAt",
        "RegisteredAt",
        "ConsumedAt",
        "RevokedAt",
        "VerifiedAt",
        "BookedAt",
        "ReleasedAt",
        "CancelledAt",
        "CompletedAt",
        "SentAt",
        "DeliveredAt",
        "DueAt",
        "ScheduledAt",
        "ProcessedAt",
        "ReceivedAt",
        "CapturedAt",
        "EscalatedAt",
        "ResolvedAt",
        "DetectedAt",
        "LastCallAt",
        "LastOccurrence",
        "Timestamp"
    ];

    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        v => ToUtc(v),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcDateTimeConverter = new(
        v => v.HasValue ? ToUtc(v.Value) : null,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

    public static ModelBuilder ConfigureUtcDateTimeColumns(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                ConfigureUtcInstantProperty(property);
            }
        }

        return modelBuilder;
    }

    public static ModelBuilder ConfigureEntitySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(IsEntityType))
        {
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression deletedAtProperty = Expression.Property(parameter, nameof(Entity.DeletedAt));
            BinaryExpression notDeletedExpression = Expression.Equal(
                deletedAtProperty,
                Expression.Constant(null, typeof(DateTime?)));

            LambdaExpression softDeleteFilter = Expression.Lambda(notDeletedExpression, parameter);
#pragma warning disable CS0618 // EF 10 still uses this API for anonymous filters, including Finbuckle-composed filters.
            LambdaExpression? existingFilter = entityType.GetQueryFilter();
#pragma warning restore CS0618

            if (existingFilter is null)
            {
                entityType.SetQueryFilter(EntityQueryFilterNames.SoftDelete, softDeleteFilter);
                continue;
            }

            Expression existingBody = new ParameterReplacingExpressionVisitor(existingFilter.Parameters[0], parameter)
                .Visit(existingFilter.Body)!;
            entityType.SetQueryFilter(Expression.Lambda(Expression.AndAlso(existingBody, notDeletedExpression), parameter));
        }

        return modelBuilder;
    }

    private sealed class ParameterReplacingExpressionVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
    }

    private static bool IsEntityType(IMutableEntityType entityType) => typeof(Entity).IsAssignableFrom(entityType.ClrType);

    private static void ConfigureUtcInstantProperty(IMutableProperty property)
    {
        if (!IsUtcInstantProperty(property))
        {
            return;
        }

        property.SetColumnType(PostgreSqlUtcInstantColumnType);

        if (property.GetValueConverter() is not null)
        {
            return;
        }

        if (property.ClrType == typeof(DateTime))
        {
            property.SetValueConverter(UtcDateTimeConverter);
        }
        else if (property.ClrType == typeof(DateTime?))
        {
            property.SetValueConverter(NullableUtcDateTimeConverter);
        }
    }

    private static bool IsUtcInstantProperty(IMutableProperty property) =>
        (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)) &&
        (UtcInstantPropertyNames.Contains(property.Name) || property.Name.EndsWith("At", StringComparison.Ordinal));

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
