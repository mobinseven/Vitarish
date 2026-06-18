using Microsoft.Extensions.Localization;

namespace Vitarish.Clinic.Interface.Models;

internal static class PatientAdvancedSearchSummary
{
    public static string Build(PatientAdvancedSearchContext context, IStringLocalizer localizer)
    {
        List<string> parts = [];

        AddCriteria(parts, localizer["PatientName"].Value, context.SearchQuery);
        AddCriteria(parts, localizer["IdentificationCode"].Value, context.SearchIdentificationCode);
        AddCriteria(parts, localizer["DateOfBirth"].Value, context.SearchDateOfBirth?.ToShortDateString());
        AddCriteria(parts, localizer["Phone"].Value, context.SearchPhone);
        AddCriteria(parts, localizer["Email"].Value, context.SearchEmail);

        return parts.Count == 0 ? localizer["NoCriteria"].Value : string.Join(" · ", parts);
    }

    private static void AddCriteria(List<string> parts, string label, string? value)
    {
        if(!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }
}
