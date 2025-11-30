using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Vitarish.Web.Users.Client.Domain.Attributes;

public class PhoneNumberAttribute : ValidationAttribute
{
    private const string Pattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{2}[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{9}$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string? valueAsString = value as string;
        if(string.IsNullOrWhiteSpace(valueAsString))
        {
            return new ValidationResult("Phone number is required.");
        }

        if(Regex.IsMatch(valueAsString, Pattern))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Phone number must include a country code and 9 digits.");
    }
}
