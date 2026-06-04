namespace Vitarish.Interfaces.Accounts.Services;

public static class VerificationSmsShieldKeys
{
    public static string ForPhoneLogin(string phoneNumber) => Build("login", null, phoneNumber);

    public static string ForPhoneChange(string userId, string phoneNumber) => Build("phone-change", userId, phoneNumber);

    private static string Build(string purpose, string? userId, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Verification SMS shield purpose is required.", nameof(purpose));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Verification SMS shield phone number is required.", nameof(phoneNumber));
        }

        string normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
        {
            throw new ArgumentException("Verification SMS shield phone number must contain digits or a plus sign.", nameof(phoneNumber));
        }

        return string.IsNullOrWhiteSpace(userId)
            ? $"{purpose}:{normalizedPhoneNumber}"
            : $"{purpose}:{userId.Trim()}:{normalizedPhoneNumber}";
    }

    private static string NormalizePhoneNumber(string phoneNumber) =>
        new([.. phoneNumber.Where(static character => char.IsDigit(character) || character == '+')]);
}
