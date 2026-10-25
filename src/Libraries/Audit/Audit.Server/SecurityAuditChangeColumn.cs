using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

/// <summary>
/// Stores an event's typed field changes as one bounded JSON text column. Rows without changes, including
/// every row written before the column existed, keep a null column.
/// </summary>
internal static class SecurityAuditChangeColumn
{
    public const int MaximumLength = 8192;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static ValueConverter<IReadOnlyList<SecurityAuditChange>, string> Converter { get; } = new(
        changes => JsonSerializer.Serialize(changes, SerializerOptions),
        json => JsonSerializer.Deserialize<SecurityAuditChange[]>(json, SerializerOptions) ?? Array.Empty<SecurityAuditChange>());

    public static ValueComparer<IReadOnlyList<SecurityAuditChange>> Comparer { get; } = new(
        (left, right) => left!.SequenceEqual(right!),
        changes => changes.Aggregate(0, (hash, change) => HashCode.Combine(hash, change)),
        changes => changes.ToArray());
}
