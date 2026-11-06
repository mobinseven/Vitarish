namespace Vitarish.Authentication.Application.Services;

public static class AuthenticationDestinationMasking
{
    public static string Phone(string phoneNumber)
    {
        string lastDigits = new(phoneNumber.Where(char.IsDigit).TakeLast(4).ToArray());
        return $"*******{lastDigits}";
    }

    public static string Email(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***";

        string localPart = email[..atIndex];
        string suffix = new(localPart.Where(char.IsLetterOrDigit).TakeLast(2).ToArray());
        return suffix.Length == 0 ? "***@***" : $"***{suffix}@***";
    }
}
