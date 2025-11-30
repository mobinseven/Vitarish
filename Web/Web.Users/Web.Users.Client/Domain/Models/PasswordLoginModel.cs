using System.ComponentModel.DataAnnotations;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class PasswordLoginModel
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
