using System.Globalization;
using System.Text;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal static class SecurityAuditCsvFormatter
{
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
        "redactedReason"
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
                row.RedactedReason
            ]);
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
    }

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
