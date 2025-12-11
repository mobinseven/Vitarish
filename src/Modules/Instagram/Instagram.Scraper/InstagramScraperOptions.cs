using System.ComponentModel.DataAnnotations;

namespace Vitarish.Instagram.Scraper;

public sealed class InstagramScraperOptions
{
    [Required]
    public string ApifyToken { get; init; } = string.Empty;
}
