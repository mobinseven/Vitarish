namespace Vitarish.Interfaces.Accounts.Components.Account;

public class PasswordChangeModel
{
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Password]
    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessageResourceName = "ConfirmPasswordError", ErrorMessageResourceType = typeof(Strings))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
