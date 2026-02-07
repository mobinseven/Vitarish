namespace Vitarish.Interfaces.Accounts.Models;

public class EmailChangeVerifyModel : VerifyCodeModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
