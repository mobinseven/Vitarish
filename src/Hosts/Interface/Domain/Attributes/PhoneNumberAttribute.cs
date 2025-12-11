namespace Vitarish.Interface.Domain.Attributes;

internal class PhoneNumberAttribute : ValidationAttribute
{
    private readonly string _pattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{2}[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{9}$";
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        IStringLocalizer<Strings> localizer = validationContext.CreateScope().ServiceProvider.GetService<IStringLocalizer<Strings>>()!;
        string valueAsString = value as string;
        return string.IsNullOrWhiteSpace(valueAsString)
            ? new ValidationResult(localizer["EmptyFieldError", localizer["PhoneNumber"]])
            : Regex.IsMatch(valueAsString, _pattern) ? ValidationResult.Success : new ValidationResult(localizer["PhoneNumberValidator"]);
    }
}
