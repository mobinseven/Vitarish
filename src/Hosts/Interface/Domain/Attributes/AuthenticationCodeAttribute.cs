namespace Vitarish.Interface.Domain.Attributes;

internal class AuthenticationCodeAttribute : ValidationAttribute
{
    private readonly string _pattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{6}$";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        IStringLocalizer<Strings>? localizer = validationContext.CreateScope().ServiceProvider.GetService<IStringLocalizer<Strings>>();
        string valueAsString = value as string;
        return string.IsNullOrWhiteSpace(valueAsString)
            ? new ValidationResult(localizer["EmptyFieldError", localizer["Code"]])
            : Regex.IsMatch(valueAsString, _pattern)
            ? ValidationResult.Success
            : new ValidationResult(localizer!["AuthenticationCodeValidator"]);
    }
}
