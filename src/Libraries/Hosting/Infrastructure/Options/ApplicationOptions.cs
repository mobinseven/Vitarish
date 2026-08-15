using System.ComponentModel.DataAnnotations;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Application-wide configuration options owned by the main host.
/// </summary>
public class ApplicationOptions
{
    /// <summary>
    /// The configuration section name for application options.
    /// </summary>
    public static string Section => Module.ApplicationSection;

    [Required, Url]
    public string LandingUrl { get; set; } = string.Empty;

    /// <summary>
    /// Feature-flag switches for in-development functionality.
    /// </summary>
    public FeaturesOptions Features { get; set; } = new();
}

