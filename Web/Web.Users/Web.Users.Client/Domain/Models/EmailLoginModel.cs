using System.ComponentModel.DataAnnotations;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class EmailLoginModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
