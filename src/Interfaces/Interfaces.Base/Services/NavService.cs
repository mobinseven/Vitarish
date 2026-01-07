using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Vitarish.Interfaces.Base.Services;

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
    void Apply(BottomNavOptions? options);

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

/// <summary>
/// Represents the current state of the bottom navigation bar.
/// </summary>
/// <param name="Home">The Home button configuration.</param>
/// <param name="Search">The Search button configuration.</param>
/// <param name="Back">The Back button configuration.</param>
/// <param name="Profile">The Profile button configuration.</param>
public sealed record BottomNavState(
    NavButton Home,
    NavButton Search,
    NavButton Back,
    NavButton Profile
);

/// <summary>
/// Represents a single navigation button.
/// </summary>
/// <param name="Visible">Whether the button is visible.</param>
/// <param name="Enabled">Whether the button is enabled (clickable).</param>
/// <param name="OnClick">The action to execute when the button is clicked.</param>
public sealed record NavButton(
    bool Visible,
    bool Enabled,
    Func<Task> OnClick
);

/// <summary>
/// Global default configuration for the bottom navigation bar.
/// </summary>
/// <param name="HomeUri">The default URI for the Home button.</param>
/// <param name="ProfileUri">The URI for the Profile button.</param>
/// <param name="SearchUri">The default URI for the Search button when not overridden.</param>
public sealed record BottomNavDefaults(
    string HomeUri,
    string ProfileUri,
    string SearchUri
)
{
    /// <summary>
    /// Gets the default configuration with common URI values.
    /// </summary>
    public static BottomNavDefaults Default { get; } = new("/", "/profile", "/search");
}

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

/// <summary>
/// Configuration for the Back button.
/// </summary>
/// <param name="Visible">Whether the Back button should be visible.</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
/// <param name="Action">The custom action to execute when clicked (takes precedence over Uri).</param>
public sealed record BackTarget(
    bool Visible = true,
    string? Uri = null,
    Func<Task>? Action = null
);

/// <summary>
/// Configuration for the Search button.
/// </summary>
/// <param name="Visible">Whether the Search button is visible.</param>
/// <param name="Enabled">Whether the Search button is enabled.</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
/// <param name="Action">Custom action to execute when clicked (takes precedence over Uri).</param>
public sealed record SearchAction(
    bool Visible = true,
    bool Enabled = true,
    string? Uri = null,
    Func<Task>? Action = null
);

/// <summary>
/// Implementation of the bottom navigation service.
/// </summary>
[RegisterScoped]
public sealed class NavService : INavService, IDisposable
{
    private readonly NavigationManager navigationManager;
    private readonly BottomNavDefaults defaults;
    private BottomNavOptions? currentOptions;

    public NavService(
        NavigationManager navigationManager,
        BottomNavDefaults? defaults = null)
    {
        this.navigationManager = navigationManager;
        this.defaults = defaults ?? BottomNavDefaults.Default;
        Current = CreateDefaultState();
        currentOptions = null;

        navigationManager.LocationChanged += HandleLocationChanged;
    }

    public event Action? Changed;

    public BottomNavState Current { get; private set; }

    public void Apply(BottomNavOptions? options)
    {
        // Compare the options to see if state really changed
        if (AreOptionsEqual(currentOptions, options))
        {
            return;
        }

        currentOptions = options;
        Current = CreateState(options);
        Changed?.Invoke();
    }

    public void ResetToDefaults() => Apply(null);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) => ResetToDefaults();

    private static bool AreOptionsEqual(BottomNavOptions? a, BottomNavOptions? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        // Compare individual properties
        if (a.HomeUriOverride != b.HomeUriOverride)
        {
            return false;
        }

        // Compare Back
        if (!AreBackTargetsEqual(a.Back, b.Back))
        {
            return false;
        }

        if (!AreSearchActionsEqual(a.Search, b.Search))
        {
            return false;
        }

        // Compare Search URI overrides
        return a.SearchUriOverride == b.SearchUriOverride;
    }

    private static bool AreBackTargetsEqual(BackTarget? a, BackTarget? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        // Note: Action functions are compared by reference equality.
        // This means passing a new lambda with the same logic will be treated as different.
        // This is intentional to keep the implementation simple and avoid complex equality checks.
        return a.Visible == b.Visible &&
               a.Uri == b.Uri &&
               ReferenceEquals(a.Action, b.Action);
    }

    private static bool AreSearchActionsEqual(SearchAction? a, SearchAction? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Visible == b.Visible &&
               a.Enabled == b.Enabled &&
               a.Uri == b.Uri &&
               ReferenceEquals(a.Action, b.Action);
    }

    private BottomNavState CreateDefaultState() => CreateState(null);

    private BottomNavState CreateState(BottomNavOptions? options)
    {
        // Home button: always visible, uses HomeUri override or default
        string homeUri = options?.HomeUriOverride ?? defaults.HomeUri;
        NavButton home = new(
            Visible: true,
            Enabled: true,
            OnClick: () =>
            {
                navigationManager.NavigateTo(homeUri);
                return Task.CompletedTask;
            }
        );

        NavButton search = CreateSearchButton(options);

        NavButton back;
        if (options?.Back is { Visible: true } backOptions)
        {
            if (backOptions.Action is not null)
            {
                back = new(
                    Visible: true,
                    Enabled: true,
                    OnClick: backOptions.Action
                );
            }
            else if (backOptions.Uri is not null)
            {
                string backUri = backOptions.Uri;
                back = new(
                    Visible: true,
                    Enabled: true,
                    OnClick: () =>
                    {
                        navigationManager.NavigateTo(backUri);
                        return Task.CompletedTask;
                    }
                );
            }
            else
            {
                back = new(
                    Visible: true,
                    Enabled: true,
                    OnClick: NavigateUpOneLevelAsync
                );
            }
        }
        else if (options?.Back is { Visible: false })
        {
            back = new(
                Visible: false,
                Enabled: false,
                OnClick: () => Task.CompletedTask
            );
        }
        else
        {
            back = new(
                Visible: false,
                Enabled: false,
                OnClick: () => Task.CompletedTask
            );
        }

        // Profile button: always visible, navigates to profile URI
        NavButton profile = new(
            Visible: true,
            Enabled: true,
            OnClick: () =>
            {
                navigationManager.NavigateTo(defaults.ProfileUri);
                return Task.CompletedTask;
            }
        );

        return new BottomNavState(home, search, back, profile);
    }

    private Task NavigateUpOneLevelAsync()
    {
        string absolutePath = new Uri(navigationManager.Uri).AbsolutePath;
        string normalizedPath = absolutePath.EndsWith("/")
            ? absolutePath.TrimEnd('/')
            : absolutePath;

        if (string.IsNullOrEmpty(normalizedPath) || normalizedPath == "/")
        {
            navigationManager.NavigateTo("/");
            return Task.CompletedTask;
        }

        int lastSlashIndex = normalizedPath.LastIndexOf('/');
        string parentPath = lastSlashIndex <= 0 ? "/" : normalizedPath[..lastSlashIndex];
        navigationManager.NavigateTo(parentPath);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    public string GetSearchUri(string? query)
    {
        string baseUri = currentOptions?.Search?.Uri
            ?? currentOptions?.SearchUriOverride
            ?? defaults.SearchUri;

        if (string.IsNullOrWhiteSpace(query))
        {
            return baseUri;
        }

        string separator = baseUri.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUri}{separator}q={Uri.EscapeDataString(query)}";
    }

    public void UpdateSearchUri(string searchUri)
    {
        BottomNavOptions merged = currentOptions is null
            ? new BottomNavOptions(SearchUriOverride: searchUri)
            : currentOptions with
            {
                Search = null,
                SearchUriOverride = searchUri
            };

        Apply(merged);
    }

    private NavButton CreateSearchButton(BottomNavOptions? options)
    {
        if (options?.Search is { } searchAction)
        {
            if (!searchAction.Visible)
            {
                return new NavButton(
                    Visible: false,
                    Enabled: false,
                    OnClick: () => Task.CompletedTask
                );
            }

            if (searchAction.Action is not null)
            {
                return new NavButton(
                    Visible: searchAction.Visible,
                    Enabled: searchAction.Enabled,
                    OnClick: searchAction.Action
                );
            }

            string actionUri = searchAction.Uri ?? defaults.SearchUri;
            return new NavButton(
                Visible: searchAction.Visible,
                Enabled: searchAction.Enabled,
                OnClick: () =>
                {
                    navigationManager.NavigateTo(actionUri);
                    return Task.CompletedTask;
                }
            );
        }

        string searchUri = options?.SearchUriOverride ?? defaults.SearchUri;
        return new NavButton(
            Visible: true,
            Enabled: true,
            OnClick: () =>
            {
                navigationManager.NavigateTo(searchUri);
                return Task.CompletedTask;
            }
        );
    }
}
