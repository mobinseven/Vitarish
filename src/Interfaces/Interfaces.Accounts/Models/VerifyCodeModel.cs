namespace Vitarish.Interfaces.Accounts.Models;

public class VerifyCodeModel
{
    public string UserId { get; set; } = string.Empty;
    [Required]
    public string Code { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}
