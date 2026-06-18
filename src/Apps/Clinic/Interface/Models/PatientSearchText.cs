namespace Vitarish.Clinic.Interface.Models;

internal static class PatientSearchText
{
    public static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());

    public static IReadOnlyList<string> Tokenize(string? value)
        => Normalize(value)?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

    public static string GetCurrentIdentificationCode(PatientAdvancedSearchContext context)
        => Normalize(context.LastLookedUpIdentificationCode)
           ?? Normalize(context.IdentificationCode)
           ?? string.Empty;
}
