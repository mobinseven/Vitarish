namespace Vitarish.Accounts.Domain.Models;

public class VerifyCodeModel
{
    public string UserId { get; set; } = string.Empty;
    [Required]
    public string Code { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; } = string.Empty;
    public string? FailureUrl { get; set; } = string.Empty;
}
