namespace Vitarish.Interfaces.Base.Constants;

/// <summary>
/// Global default configuration for the bottom navigation bar.
/// </summary>
/// <param name="HomeUri">The default URI for the Home button.</param>
/// <param name="ProfileUri">The URI for the Profile button.</param>
/// <param name="SearchUri">The default URI for the Search button when not overridden.</param>
public sealed record NavDefaults(
    string HomeUri,
    string ProfileUri,
    string SearchUri
)
{
    /// <summary>
    /// Gets the default configuration with common URI values.
    /// </summary>
    public static NavDefaults Default { get; } = new("/", "/profile", "/search");
}
