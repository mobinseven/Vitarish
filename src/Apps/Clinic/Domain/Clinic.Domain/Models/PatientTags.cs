namespace Vitarish.Clinic.Domain.Models;

public static class PatientTags
{
    public static string[] Normalize(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        List<string> normalized = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string? tag in tags)
        {
            string value = tag?.Trim() ?? string.Empty;
            if (value.Length > 0 && seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return [.. normalized];
    }
}
