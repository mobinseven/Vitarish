namespace Vitarish.Web.Users.Domain.Attributes;

using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Vitarish.Web.Users.Infrastructure.Localization;

internal class AuthenticationCodeAttribute : ValidationAttribute
{
    private readonly string _pattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{6}$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        IStringLocalizer<Strings>? localizer = validationContext.CreateScope().ServiceProvider.GetService<IStringLocalizer<Strings>>();
        string valueAsString = value as string;
        if(string.IsNullOrWhiteSpace(valueAsString))
        {
            return new ValidationResult(localizer["EmptyFieldError", localizer["Code"]]);
        }
        if(Regex.IsMatch(valueAsString, _pattern))
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(localizer!["AuthenticationCodeValidator"]);
    }
}
