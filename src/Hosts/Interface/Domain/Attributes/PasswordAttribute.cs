namespace Vitarish.Interface.Domain.Attributes;

public class PasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string password = value as string;

        if (string.IsNullOrEmpty(password))
        {
            return new ValidationResult(null);
        }

        IStringLocalizer<Strings> localizer = validationContext.CreateScope().ServiceProvider.GetService<IStringLocalizer<Strings>>()!;

        if (password.Length < 6)
        {
            return new ValidationResult(localizer["PasswordLengthError"]);
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new ValidationResult(localizer["PasswordUppercaseError"]);
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return new ValidationResult(localizer["PasswordLowercaseError"]);
        }

        return !Regex.IsMatch(password, @"\d")
            ? new ValidationResult(localizer["PasswordDigitError"])
            : !Regex.IsMatch(password, @"[\W_]") ? new ValidationResult(localizer["PasswordSignError"]) : ValidationResult.Success;
    }
}
