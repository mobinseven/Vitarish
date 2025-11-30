using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Vitarish.Web.Users.Client.Domain.Attributes;

public class AuthenticationCodeAttribute : ValidationAttribute
{
    private const string Pattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{6}$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string? valueAsString = value as string;
        if(string.IsNullOrWhiteSpace(valueAsString))
        {
            return new ValidationResult("Authentication code is required.");
        }

        if(Regex.IsMatch(valueAsString, Pattern))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Authentication code must be a 6 digit number.");
    }
}
