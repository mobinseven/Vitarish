using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

/// <summary>
/// Provides centralized configuration for bottom navigation bar buttons.
/// </summary>
public interface INavService
{
    void Back();
    void Home();
    void Profile();

    Breadcrumb[] Breadcrumbs { get; }

    event EventHandler RenderRequired;

    /// <summary>
    /// Builds a search URI using the configured defaults and the supplied query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>The resolved search URI.</returns>
    string GetSearchUri(string? query);
    void OverridePageTitle(string text,string title);
}
