namespace Vitarish.Clinic.Interface.Models;

internal static class PatientSearchMatcher
{
    public static bool MatchesAdvancedSearch(ProjectReadDto project, PatientAdvancedSearchCriteria criteria)
        => MatchesName(project.Name, criteria.NameTokens)
           && MatchesIdentificationCode(project, criteria.IdentificationCode)
           && MatchesDate(project.GetField<DateTime?>(nameof(PatientReadDto.DateOfBirth)), criteria.DateOfBirth)
           && MatchesPhone(project.GetField<string>(nameof(PatientReadDto.Phone)), criteria)
           && MatchesText(project.GetField<string>(nameof(PatientReadDto.Email)), criteria.Email);

    public static bool IsExactIdentificationCode(ProjectReadDto project, string identificationCode)
        => string.Equals(project.IdentificationCode?.Trim(), identificationCode, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesName(string? value, IReadOnlyList<string> expectedTokens)
    {
        if(expectedTokens.Count == 0)
        {
            return true;
        }

        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return expectedTokens.All(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesIdentificationCode(ProjectReadDto project, string? identificationCode)
        => identificationCode is null || IsExactIdentificationCode(project, identificationCode);

    private static bool MatchesDate(DateTime? value, DateTime? expected)
        => !expected.HasValue || (value.HasValue && value.Value.Date == expected.Value.Date);

    private static bool MatchesText(string? value, string? expected)
        => expected is null
           || (!string.IsNullOrWhiteSpace(value) && value.Contains(expected, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesPhone(string? value, PatientAdvancedSearchCriteria criteria)
    {
        if(criteria.Phone is null)
        {
            return true;
        }

        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if(criteria.PhoneDigits.Length > 0)
        {
            string valueDigits = PatientSearchText.DigitsOnly(value);
            return valueDigits.Contains(criteria.PhoneDigits, StringComparison.Ordinal);
        }

        return value.Contains(criteria.Phone, StringComparison.OrdinalIgnoreCase);
    }
}
