using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Vitarish.Web.Users.Client.Domain.Attributes;

public class PasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string? password = value as string;

        if(string.IsNullOrEmpty(password))
        {
            return new ValidationResult(null);
        }

        if(password.Length < 6)
        {
            return new ValidationResult("Password must be at least 6 characters long.");
        }

        if(!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new ValidationResult("Password must contain an uppercase letter.");
        }

        if(!Regex.IsMatch(password, @"[a-z]"))
        {
            return new ValidationResult("Password must contain a lowercase letter.");
        }

        if(!Regex.IsMatch(password, @"\d"))
        {
            return new ValidationResult("Password must contain a digit.");
        }

        if(!Regex.IsMatch(password, @"[\W_]"))
        {
            return new ValidationResult("Password must contain a symbol.");
        }

        return ValidationResult.Success;
    }
}
