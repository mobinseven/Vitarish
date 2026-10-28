using System.Globalization;
using System.Text;
using System.Text.Json;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal static class SecurityAuditCsvFormatter
{
    private static readonly JsonSerializerOptions ChangeSerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly string[] Headers =
    [
        "id",
        "tenantId",
        "eventCategory",
        "eventName",
        "operation",
        "outcome",
        "actorKind",
        "actorId",
        "subjectId",
        "resource",
        "occurredAt",
        "correlationId",
        "sourceBoundary",
        "sourceIpKind",
        "sourceIpAddress",
        "clientInterfaceHost",
        "redactedReason",
        "resourceNameHash",
        "resourceExtension",
        "resourceSize",
        "changes",
        "resourceName"
    ];

    public static byte[] Format(IReadOnlyList<SecurityAuditEventReadDto> rows)
    {
        StringBuilder csv = new();
        AppendRow(csv, Headers);
        foreach (SecurityAuditEventReadDto row in rows)
        {
            AppendRow(csv,
            [
                row.Id.ToString(),
                row.TenantId,
                row.EventCategory,
                row.EventName,
                row.Operation,
                row.Outcome,
                row.ActorKind,
                row.ActorId,
                row.SubjectId,
                row.Resource,
                row.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
                row.CorrelationId,
                row.SourceBoundary,
                row.SourceIpKind,
                row.SourceIpAddress,
                row.ClientInterfaceHost,
                row.RedactedReason,
                row.ResourceNameHash,
                row.ResourceExtension,
                row.ResourceSize?.ToString(CultureInfo.InvariantCulture),
                FormatChanges(row.Changes ?? []),
                row.ResourceName
            ]);
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
    }

    internal static string FormatChanges(IReadOnlyList<SecurityAuditChange> changes) =>
        JsonSerializer.Serialize(
            changes.Select(change => new
            {
                field = change.Field,
                previousValue = change.PreviousValue ?? SecurityAuditEventCatalog.ChangeValueNone,
                newValue = change.NewValue ?? SecurityAuditEventCatalog.ChangeValueNone
            }),
            ChangeSerializerOptions);

    private static void AppendRow(StringBuilder csv, IReadOnlyList<string?> values)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (index > 0)
                _ = csv.Append(',');
            _ = csv.Append(Escape(values[index]));
        }
        _ = csv.Append("\r\n");
    }

    private static string Escape(string? value)
    {
        string normalized = value ?? string.Empty;
        if (normalized.Length > 0 && normalized[0] is '=' or '+' or '-' or '@')
            normalized = "'" + normalized;
        return $"\"{normalized.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
