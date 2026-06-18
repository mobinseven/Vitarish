namespace Vitarish.Clinic.Interface.Models;

internal sealed record PatientAdvancedSearchCriteria(
    string? Name,
    IReadOnlyList<string> NameTokens,
    string? IdentificationCode,
    DateTime? DateOfBirth,
    string? Phone,
    string PhoneDigits,
    string? Email)
{

    public bool NeedsLargeClientSideSearch
        => DateOfBirth.HasValue
           || Phone is not null
           || Email is not null;

    public static bool HasAny(PatientAdvancedSearchContext context)
        => !string.IsNullOrWhiteSpace(context.SearchQuery)
           || !string.IsNullOrWhiteSpace(context.SearchIdentificationCode)
           || context.SearchDateOfBirth.HasValue
           || !string.IsNullOrWhiteSpace(context.SearchPhone)
           || !string.IsNullOrWhiteSpace(context.SearchEmail);

    public static PatientAdvancedSearchCriteria From(PatientAdvancedSearchContext context)
    {
        string? name = PatientSearchText.Normalize(context.SearchQuery);
        string? phone = PatientSearchText.Normalize(context.SearchPhone);

        return new(
            name,
            PatientSearchText.Tokenize(name),
            PatientSearchText.Normalize(context.SearchIdentificationCode),
            context.SearchDateOfBirth?.Date,
            phone,
            phone is null ? string.Empty : PatientSearchText.DigitsOnly(phone),
            PatientSearchText.Normalize(context.SearchEmail));
    }
}
