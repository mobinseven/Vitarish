using System.ComponentModel.DataAnnotations;
using Vitarish.Web.Users.Client.Domain.Attributes;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class AuthenticationCodeModel
{
    [Required]
    [AuthenticationCode]
    public string Code { get; set; } = string.Empty;
}
