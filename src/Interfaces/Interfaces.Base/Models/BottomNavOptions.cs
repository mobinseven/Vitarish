namespace Vitarish.Interfaces.Base.Models;

/// <summary>
/// Per-page configuration options for the bottom navigation bar.
/// </summary>
/// <param name="HomeUriOverride">Optional override for the Home button URI.</param>
/// <param name="Back">Optional Back button configuration.</param>
/// <param name="SearchUriOverride">Optional override for the Search button URI.</param>
public sealed record BottomNavOptions(
    string? HomeUriOverride = null,
    BackTarget? Back = null,
    string? SearchUriOverride = null,
    SearchAction? Search = null
);
