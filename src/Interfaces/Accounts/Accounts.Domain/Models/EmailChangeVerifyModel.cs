using System.ComponentModel.DataAnnotations;

namespace Vitarish.Accounts.Domain.Models;

public class EmailChangeVerifyModel : VerifyCodeModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
