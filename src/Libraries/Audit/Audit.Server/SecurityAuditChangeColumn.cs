using System.Text.Encodings.Web;
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

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static ValueConverter<IReadOnlyList<SecurityAuditChange>, string> Converter { get; } = new(
        changes => Serialize(changes),
        json => JsonSerializer.Deserialize<SecurityAuditChange[]>(json, SerializerOptions) ?? Array.Empty<SecurityAuditChange>());

    public static ValueComparer<IReadOnlyList<SecurityAuditChange>> Comparer { get; } = new(
        (left, right) => left!.SequenceEqual(right!),
        changes => changes.Aggregate(0, (hash, change) => HashCode.Combine(hash, change)),
        changes => changes.ToArray());

    internal static string Serialize(IReadOnlyList<SecurityAuditChange> changes) =>
        JsonSerializer.Serialize(changes, SerializerOptions);

    internal static bool IsWithinLength(IReadOnlyList<SecurityAuditChange> changes) =>
        Serialize(changes).Length <= MaximumLength;

    /// <summary>Shortens change values until the serialized column fits its configured length.</summary>
    internal static IReadOnlyList<SecurityAuditChange> Fit(IReadOnlyList<SecurityAuditChange> changes)
    {
        if (changes.Count == 0 || IsWithinLength(changes))
            return changes;

        SecurityAuditChange[] bounded = changes.ToArray();
        while (Serialize(bounded).Length > MaximumLength)
        {
            int target = LongestValue(bounded);
            if (target < 0)
                break;

            int index = target / 2;
            bool previous = target % 2 == 0;
            SecurityAuditChange change = bounded[index];
            string value = (previous ? change.PreviousValue : change.NewValue)!;
            string shortened = TrimOne(value);
            bounded[index] = previous
                ? change with { PreviousValue = shortened }
                : change with { NewValue = shortened };
        }

        return bounded;
    }

    private static int LongestValue(IReadOnlyList<SecurityAuditChange> changes)
    {
        int best = -1;
        int bestLength = 0;
        for (int index = 0; index < changes.Count; index++)
        {
            string? previous = changes[index].PreviousValue;
            if (previous is { Length: > 0 } && previous.Length > bestLength)
            {
                best = index * 2;
                bestLength = previous.Length;
            }

            string? current = changes[index].NewValue;
            if (current is { Length: > 0 } && current.Length > bestLength)
            {
                best = index * 2 + 1;
                bestLength = current.Length;
            }
        }

        return best;
    }

    private static string TrimOne(string value)
    {
        int length = value.Length - 1;
        if (length > 0 && char.IsHighSurrogate(value[length - 1]))
            length--;
        return value[..length];
    }
}
