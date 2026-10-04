namespace Vitarish.Users.Tests.Security;

/// <summary>
/// The single table of phone-normalization cases. The C# rule test and the
/// PostgreSQL agreement test both apply these rows, so the conversion rule and
/// its SQL mirror cannot drift apart silently.
/// </summary>
internal static class PhoneNormalizationCases
{
    internal static readonly (string Input, string? Expected)[] All =
    [
        ("09123456789", "+989123456789"),
        ("9123456789", "+989123456789"),
        ("۰۹۱۲۳۴۵۶۷۸۹", "+989123456789"),
        ("٠٩١٢٣٤٥٦٧٨٩", "+989123456789"),
        ("0098 912 345 6789", "+989123456789"),
        ("+98 (912) 345-6789", "+989123456789"),
        ("989123456789", "+989123456789"),
        ("+989123456789", "+989123456789"),
        ("\u200E09123456789\u200F", "+989123456789"),
        ("0912\u00A03456789", "+989123456789"),
        ("+14155552671", "+14155552671"),
        ("02112345678", null),
        ("+982112345678", null),
        ("0912345678", null),
        ("abc", null),
        ("+0123", null),
        ("+1234567890123456", null),
    ];
}
