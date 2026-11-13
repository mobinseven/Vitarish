namespace Vitarish.Clinic.Application.Assistant.Staff;

/// <summary>
/// The one masking of a phone number or email for everything that is not the owner's own review: the tool result
/// the model sees, logs and telemetry.
/// </summary>
public static class AssistantContactMasking
{
    private const char Mask = '•';
    private const int PhoneMaskedLength = 5;
    private const int PhoneVisibleTail = 4;
    private const int PhoneVisiblePrefix = 3;
    private const int EmailMaskedLength = 3;

    public static string Phone(string? phoneNumber)
    {
        string value = phoneNumber?.Trim() ?? string.Empty;
        if (value.Length <= PhoneVisiblePrefix + PhoneVisibleTail)
            return new string(Mask, PhoneMaskedLength);
        string prefix = value[0] == '+' ? value[..PhoneVisiblePrefix] : string.Empty;
        return prefix + new string(Mask, PhoneMaskedLength) + value[^PhoneVisibleTail..];
    }

    public static string Email(string? email)
    {
        string value = email?.Trim() ?? string.Empty;
        int at = value.IndexOf('@');
        return at <= 0
            ? new string(Mask, EmailMaskedLength)
            : value[0] + new string(Mask, EmailMaskedLength) + value[at..];
    }

    public static string Contact(string? phoneNumber, string? email) =>
        string.IsNullOrWhiteSpace(email) ? Phone(phoneNumber) : Email(email);
}
