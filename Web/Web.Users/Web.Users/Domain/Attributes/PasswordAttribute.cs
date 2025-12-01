using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Vitarish.Web.Users.Infrastructure.Localization;

namespace Vitarish.Web.Users.Domain.Attributes;

public class PasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        string password = value as string;

        if(string.IsNullOrEmpty(password))
        {
            return new ValidationResult(null);
        }

        IStringLocalizer<Strings> localizer = validationContext.CreateScope().ServiceProvider.GetService<IStringLocalizer<Strings>>()!;

        if(password.Length < 6)
        {
            return new ValidationResult(localizer["PasswordLengthError"]);
        }

        if(!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new ValidationResult(localizer["PasswordUppercaseError"]);
        }

        if(!Regex.IsMatch(password, @"[a-z]"))
        {
            return new ValidationResult(localizer["PasswordLowercaseError"]);
        }

        if(!Regex.IsMatch(password, @"\d"))
        {
            return new ValidationResult(localizer["PasswordDigitError"]);
        }

        if(!Regex.IsMatch(password, @"[\W_]"))
        {
            return new ValidationResult(localizer["PasswordSignError"]);
        }

        return ValidationResult.Success;
    }
}
