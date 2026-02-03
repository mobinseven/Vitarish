namespace Vitarish.Interfaces.Accounts.Models;

public class PasswordLoginModel
{
    [Required]
    public string Username { get; set; } = "";
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
