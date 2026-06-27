namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

/// <summary>
/// Provides centralized configuration for bottom navigation bar buttons.
/// </summary>
public interface INavService
{
    string? SearchString { get; set; }

    Breadcrumb[] Breadcrumbs { get; }

    event EventHandler<TitleChangeEventArgs> TitleChanged;

    void Back();
    void Home();
    void Profile();
    void EnableSearch();
    void DisableSearch();
    void OverridePageTitle(string id, string title);
}
