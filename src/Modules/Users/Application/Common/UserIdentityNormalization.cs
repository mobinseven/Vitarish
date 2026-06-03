namespace Vitarish.Users.Application.Common;

public static class UserIdentityNormalization
{
    public const int IdentificationCodeMaxLength = 64;
    public const int BirthCountryCodeMaxLength = 8;
    public const int BirthCityMaxLength = 128;

    public static string? NormalizeIdentificationCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        char[] filtered = value.Trim().Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray();
        return filtered.Length == 0 ? null : new string(filtered).ToUpperInvariant();
    }
}
