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

        string normalizedPhoneNumber = phoneNumber.Trim();
        return string.IsNullOrWhiteSpace(userId)
            ? $"{purpose}:{normalizedPhoneNumber}"
            : $"{purpose}:{userId.Trim()}:{normalizedPhoneNumber}";
    }
}
