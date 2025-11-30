using System.ComponentModel.DataAnnotations;
using Vitarish.Web.Users.Client.Domain.Attributes;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class PasswordChangeModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [Password]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords must match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
