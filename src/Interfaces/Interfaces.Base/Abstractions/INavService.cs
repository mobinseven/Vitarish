using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

/// <summary>
/// Provides centralized configuration for bottom navigation bar buttons.
/// </summary>
public interface INavService
{
    /// <summary>
    /// Gets the current bottom navigation state.
    /// </summary>
    BottomNavState Current { get; }

    /// <summary>
    /// Raised when the bottom navigation configuration changes.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Applies per-page navigation options to the current state.
    /// </summary>
    /// <param name="options">The page-specific navigation options, or null to use defaults.</param>
    void Apply(NavOptions? options);

    /// <summary>
    /// Builds a search URI using the configured defaults and the supplied query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>The resolved search URI.</returns>
    string GetSearchUri(string? query);

    /// <summary>
    /// Updates only the search destination while preserving other navigation options.
    /// </summary>
    /// <param name="searchUri">The search destination URI.</param>
    void UpdateSearchUri(string searchUri);

    /// <summary>
    /// Restores the navigation bar to its global defaults.
    /// </summary>
    void ResetToDefaults();
}
