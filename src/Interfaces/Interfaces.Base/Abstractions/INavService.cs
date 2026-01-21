using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

/// <summary>
/// Provides centralized configuration for bottom navigation bar buttons.
/// </summary>
public interface INavService
{
    string? SearchString { get; set; }

    Breadcrumb[] Breadcrumbs { get; }
    void Back();
    void Home();
    void Profile();
    void EnableSearch();
    void OverridePageTitle(string text,string title);
}
