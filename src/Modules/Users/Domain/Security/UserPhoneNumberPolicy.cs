using System.Text.RegularExpressions;

namespace Vitarish.Users.Domain.Security;

public static class UserPhoneNumberPolicy
{
    public const string InvalidE164FormatMessage = "Phone number must use E.164 format.";

    private static readonly Regex E164PhoneNumber = new(@"^\+[1-9][0-9]{1,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidE164(string? phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && E164PhoneNumber.IsMatch(phoneNumber);
}
