namespace Vitarish.Interfaces.Accounts.Attributes;

public class PasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string password = value as string;

        if (string.IsNullOrEmpty(password))
        {
            return new ValidationResult(null);
        }


        if (password.Length < 6)
        {
            return new ValidationResult("PasswordLengthError");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new ValidationResult("PasswordUppercaseError");
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return new ValidationResult("PasswordLowercaseError");
        }

        return !Regex.IsMatch(password, @"\d")
            ? new ValidationResult("PasswordDigitError")
            : !Regex.IsMatch(password, @"[\W_]") ? new ValidationResult("PasswordSignError") : ValidationResult.Success;
    }
}
