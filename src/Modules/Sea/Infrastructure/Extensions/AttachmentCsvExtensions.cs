
using System.Text;

namespace Vitarish.Sea.Infrastructure.Extensions;

public static class AttachmentCsvExtensions
{
    public static string ToCsv(this IEnumerable<AttachmentCsvItem> items)
    {
        StringBuilder builder = new();

        foreach (AttachmentCsvItem item in items.Where(i => i.Attachment.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase)))
        {
            _ = builder.AppendLine(string.Join(',', [
                Escape(item.ProjectName),
                Escape(item.ProcedureName),
                Escape(item.EventName),
                Escape(item.Attachment.Content),
                item.Attachment.CreatedAt.ToString("O"),
                Escape(item.CreatedBy ?? string.Empty)]));
        }

        return builder.ToString();

        static string Escape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

public record AttachmentCsvItem(
    string ProjectName,
    string ProcedureName,
    string EventName,
    AttachmentDto Attachment,
    string? CreatedBy);

