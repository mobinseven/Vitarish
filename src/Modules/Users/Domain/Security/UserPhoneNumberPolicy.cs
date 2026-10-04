using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Vitarish.Users.Domain.Security;

public static class UserPhoneNumberPolicy
{
    public const string DefaultCountryCallingCode = "98";
    public const string InvalidPhoneNumberMessage = "Phone number must be a valid mobile number.";

    private static readonly Regex E164PhoneNumber = new(@"^\+[1-9][0-9]{1,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex IranianMobileNumber = new(@"^\+989[0-9]{9}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidE164(string? phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && E164PhoneNumber.IsMatch(phoneNumber);

    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164)
    {
        e164 = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string value = MapDigits(input.Trim());
        value = RemoveSeparators(value);
        if (value.Length == 0)
            return false;

        if (value.StartsWith("00", StringComparison.Ordinal))
            value = "+" + value[2..];

        if (value[0] != '+')
        {
            if (value.Length == 11 && value[0] == '0' && AllAsciiDigits(value))
                value = "+" + DefaultCountryCallingCode + value[1..];
            else if (value.Length == 10 && value[0] == '9' && AllAsciiDigits(value))
                value = "+" + DefaultCountryCallingCode + value;
            else if (value.Length == 12 && value[0] == '9' && value[1] == '8' && AllAsciiDigits(value))
                value = "+" + value;
            else
                return false;
        }

        if (!IsValidE164(value))
            return false;
        if (value.StartsWith("+" + DefaultCountryCallingCode, StringComparison.Ordinal) && !IranianMobileNumber.IsMatch(value))
            return false;

        e164 = value;
        return true;
    }

    private static string MapDigits(string input)
    {
        char[] characters = input.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i] = characters[i] switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)('0' + (characters[i] - '\u06F0')),
                >= '\u0660' and <= '\u0669' => (char)('0' + (characters[i] - '\u0660')),
                _ => characters[i]
            };
        }

        return new string(characters);
    }

    private static string RemoveSeparators(string value)
    {
        char[] characters = new char[value.Length];
        int length = 0;
        foreach (char character in value)
        {
            if (IsSeparator(character))
                continue;
            characters[length++] = character;
        }

        return new string(characters, 0, length);
    }

    private static bool IsSeparator(char character) =>
        character is ' ' or '\u00A0' or '-' or '.' or '(' or ')' or '\u200E' or '\u200F' or (>= '\u202A' and <= '\u202E');

    private static bool AllAsciiDigits(string value)
    {
        foreach (char character in value)
        {
            if (!char.IsAsciiDigit(character))
                return false;
        }

        return true;
    }
}
