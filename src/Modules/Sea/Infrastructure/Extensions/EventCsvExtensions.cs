
using System.Text;

namespace Vitarish.Sea.Infrastructure.Extensions;

public static class EventCsvExtensions
{
    public static string ToCsv(this IEnumerable<EventCsvItem> items)
    {
        StringBuilder builder = new();

        foreach(EventCsvItem item in items)
        {
            _ = builder.AppendLine(string.Join(',', [
                Escape(item.ProjectName),
                Escape(item.ProcedureName),
                Escape(item.Title),
                Escape(item.Description),
                item.CommittedAt?.ToString("O") ?? string.Empty
            ]));
        }

        return builder.ToString();

        static string Escape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

public record EventCsvItem(
    string ProjectName,
    string ProcedureName,
    string Title,
    string Description,
    DateTimeOffset? CommittedAt);
