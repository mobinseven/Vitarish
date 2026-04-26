
using System.Text;

namespace Vitarish.Sea.Infrastructure.Extensions;

public static class ProjectCsvExtensions
{
    public static string ToCsv(this IEnumerable<ProjectCsvItem> items)
    {
        StringBuilder builder = new();

        foreach (ProjectCsvItem item in items)
        {
            _ = builder.AppendLine(string.Join(',', [
                Escape(item.Name),
                Escape(item.Description),
                item.Template.ToString()
            ]));
        }

        return builder.ToString();

        static string Escape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

public record ProjectCsvItem(string Name, string Description, bool Template);
