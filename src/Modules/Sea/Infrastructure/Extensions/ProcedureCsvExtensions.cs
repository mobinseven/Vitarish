
using System.Text;

namespace Vitarish.Sea.Infrastructure.Extensions;

public static class ProcedureCsvExtensions
{
    public static string ToCsv(this IEnumerable<ProcedureCsvItem> items)
    {
        StringBuilder builder = new();

        foreach (ProcedureCsvItem item in items)
        {
            _ = builder.AppendLine(string.Join(',', [
                Escape(item.ProjectName),
                Escape(item.ProcedureName)
            ]));
        }

        return builder.ToString();

        static string Escape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

public record ProcedureCsvItem(string ProjectName, string ProcedureName);
