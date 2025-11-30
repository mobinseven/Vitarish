using System.ComponentModel.DataAnnotations;

namespace Vitarish.Web.Users.Client.Domain.Models;

public class UserProfileModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    [Url]
    [Display(Name = "Website")]
    public string Website { get; set; } = string.Empty;
}
