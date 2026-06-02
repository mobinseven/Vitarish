namespace Vitarish.Interfaces.Accounts.Attributes;

public class PasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string? password = value as string;

        return string.IsNullOrEmpty(password)
            ? new ValidationResult(null)
            : password.Length < 6
            ? new ValidationResult("PasswordLengthError")
            : !Regex.IsMatch(password, @"[A-Z]")
            ? new ValidationResult("PasswordUppercaseError")
            : !Regex.IsMatch(password, @"[a-z]")
            ? new ValidationResult("PasswordLowercaseError")
            : !Regex.IsMatch(password, @"\d")
            ? new ValidationResult("PasswordDigitError")
            : !Regex.IsMatch(password, @"[\W_]") ? new ValidationResult("PasswordSignError") : ValidationResult.Success;
    }
}
