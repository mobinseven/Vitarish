using System.ComponentModel.DataAnnotations;

namespace Vitarish.Accounts.Domain.Models;

public class EmailChangeVerifyModel : VerifyCodeModel
{
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
