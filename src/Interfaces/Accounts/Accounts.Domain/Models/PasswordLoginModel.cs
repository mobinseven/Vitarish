namespace Vitarish.Accounts.Domain.Models;

public class PasswordLoginModel
{
    [Required]
    public string Username { get; set; } = "";
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
