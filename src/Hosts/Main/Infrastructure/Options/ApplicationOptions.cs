using Vitarish.Communications.Domain.Options;
using Vitarish.Files.Domain.Options;
using Vitarish.Instagram.Scraper;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Aggregated application configuration options that combines all module-specific option blocks.
/// This provides a centralized configuration source for all modules.
/// </summary>
public class ApplicationOptions
{
    /// <summary>
    /// The configuration section name for application options.
    /// </summary>
    public const string Section = "Vitarish";

    /// <summary>
    /// AI/Intelligence module options.
    /// </summary>
    public AiOptions Ai { get; set; } = new();

    /// <summary>
    /// Files module options.
    /// </summary>
    public FilesOptions Files { get; set; } = new();

    /// <summary>
    /// Files module options.
    /// </summary>
    public MinioOptions Minio { get; set; } = new();

    /// <summary>
    /// Communications provider options.
    /// </summary>
    public ProvidersOptions Providers { get; set; } = new();

    /// <summary>
    /// Notification options.
    /// </summary>
    public NotificationOptions Notifications { get; set; } = new();


    public InstagramScraperOptions InstagramScraper { get; set; } = new();

}
