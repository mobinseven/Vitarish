namespace Vitarish.Medical.Domain.Models;

public static class MedicalValidationRules
{
    public static bool IsValidEffectiveDateRange(DateOnly? from, DateOnly? to)
        => from is null || to is null || from <= to;

    public static bool IsImported(RecordOrigin origin) => origin == RecordOrigin.Imported;

    public static bool IsLocal(RecordOrigin origin) => origin == RecordOrigin.Local;

    public static bool IsValidConfidence(decimal confidence) => confidence is >= 0 and <= 1;

    public static bool IsValidUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri)) return true;
        return Uri.TryCreate(uri, UriKind.Absolute, out _);
    }
}
