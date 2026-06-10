using System.Text.RegularExpressions;

public static partial class PassportValidator
{
    // Broad practical fallback:
    // Most passport document numbers fit 6-9 alphanumeric characters.
    [GeneratedRegex(@"^[A-Z0-9]{6,9}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GlobalIcaoRegex();

    // Iran: commonly one Latin letter followed by 8 digits, e.g. A12345678.
    [GeneratedRegex(@"^[A-Z][0-9]{8}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IranRegex();

    // USA, India, Australia: often one alphanumeric prefix followed by 7-8 digits.
    [GeneratedRegex(@"^[A-Z0-9][0-9]{7,8}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UsInAuRegex();

    // UK/EU-style relaxed pattern:
    // two alphanumeric prefix characters + 6-7 alphanumeric characters.
    [GeneratedRegex(@"^[A-Z0-9]{2}[A-Z0-9]{6,7}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UkEuRegex();

    // Numeric-only passports are common in some countries.
    [GeneratedRegex(@"^[0-9]{6,9}$", RegexOptions.Compiled)]
    private static partial Regex NumericRegex();

    public static bool IsValidPassport(string? passportNumber, string? countryIso3 = null)
    {
        string? sanitized = NormalizePassport(passportNumber);

        if (sanitized is null)
        {
            return false;
        }

        // Step 1: broad physical / MRZ-compatible constraint.
        if (!GlobalIcaoRegex().IsMatch(sanitized))
        {
            return false;
        }

        // Step 2: optional country-specific strict check.
        return string.IsNullOrWhiteSpace(countryIso3) || countryIso3.Trim().ToUpperInvariant() switch
        {
            "IRN" => IranRegex().IsMatch(sanitized),

            "USA" or "IND" or "AUS" =>
                UsInAuRegex().IsMatch(sanitized),

            "GBR" or "DEU" or "FRA" or "NLD" or "ESP" or "ITA" =>
                UkEuRegex().IsMatch(sanitized),

            // Soft fallback:
            // unknown country format, but globally plausible passport number.
            _ => true
        };
    }

    public static string? NormalizePassport(string? passportNumber)
    {
        if (string.IsNullOrWhiteSpace(passportNumber))
        {
            return null;
        }

        string sanitized = new string(
            passportNumber
                .Where(char.IsLetterOrDigit)
                .ToArray()
        ).ToUpperInvariant();

        return string.IsNullOrWhiteSpace(sanitized)
            ? null
            : sanitized;
    }
}
