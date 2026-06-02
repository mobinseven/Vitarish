namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal static class CalendarAnnotationCultureResolver
{
    public static ResolvedCalendarCulture Resolve(CalendarAnnotationQuery query)
    {
        CultureInfo culture = query.Culture ?? CultureInfo.CurrentCulture;
        string countryCode = NormalizeCountryCode(query.CountryCode, culture);

        return new ResolvedCalendarCulture(culture, countryCode);
    }

    private static string NormalizeCountryCode(string? countryCode, CultureInfo culture)
    {
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            return countryCode.Trim().ToUpperInvariant();
        }

        try
        {
            CultureInfo regionCulture = culture.IsNeutralCulture
                ? CultureInfo.CreateSpecificCulture(culture.Name)
                : culture;

            return new RegionInfo(regionCulture.Name).TwoLetterISORegionName.ToUpperInvariant();
        }
        catch (ArgumentException)
        {
            return "ZZ";
        }
    }
}

internal sealed record ResolvedCalendarCulture(CultureInfo Culture, string CountryCode);
