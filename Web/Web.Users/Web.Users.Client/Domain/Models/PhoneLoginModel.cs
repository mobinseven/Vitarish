using System.ComponentModel.DataAnnotations;
using Vitarish.Web.Users.Client.Domain.Attributes;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class PhoneLoginModel
{
    [Required]
    [PhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;
}
