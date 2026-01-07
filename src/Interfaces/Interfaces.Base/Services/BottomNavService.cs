using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Vitarish.Interfaces.Base.Services;

/// <summary>
/// Provides centralized configuration for bottom navigation bar buttons.
/// </summary>
public interface IBottomNavService
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
/// <param name="Search">Optional Search button configuration.</param>
public sealed record BottomNavOptions(
    string? HomeUriOverride = null,
    BackTarget? Back = null,
    SearchAction? Search = null
);

/// <summary>
/// Configuration for the Back button.
/// </summary>
/// <param name="Visible">Whether the Back button should be visible.</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
/// <param name="Action">The custom action to execute when clicked (takes precedence over Uri).</param>
public sealed record BackTarget(
    bool Visible,
    string? Uri = null,
    Func<Task>? Action = null
);

/// <summary>
/// Configuration for the Search button action.
/// </summary>
/// <param name="Action">The custom action to execute when clicked (takes precedence over Uri).</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
public sealed record SearchAction(
    Func<Task>? Action = null,
    string? Uri = null
);

/// <summary>
/// Implementation of the bottom navigation service.
/// </summary>
[RegisterScoped]
public sealed class BottomNavService : IBottomNavService, IDisposable
{
    private readonly NavigationManager navigationManager;
    private readonly BottomNavDefaults defaults;
    private BottomNavState current;
    private BottomNavOptions? currentOptions;

    public BottomNavService(NavigationManager navigationManager, BottomNavDefaults? defaults = null)
    {
        this.navigationManager = navigationManager;
        this.defaults = defaults ?? BottomNavDefaults.Default;
        current = CreateDefaultState();
        currentOptions = null;

        navigationManager.LocationChanged += HandleLocationChanged;
    }

    public event Action? Changed;

    public BottomNavState Current => current;

    public void Apply(BottomNavOptions? options)
    {
        // Compare the options to see if state really changed
        if (AreOptionsEqual(currentOptions, options))
        {
            return;
        }

        currentOptions = options;
        current = CreateState(options);
        Changed?.Invoke();
    }

    public void ResetToDefaults()
    {
        Apply(null);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        ResetToDefaults();
    }

    private bool AreOptionsEqual(BottomNavOptions? a, BottomNavOptions? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        // Compare individual properties
        if (a.HomeUriOverride != b.HomeUriOverride) return false;

        // Compare Back
        if (!AreBackTargetsEqual(a.Back, b.Back)) return false;

        // Compare Search
        if (!AreSearchActionsEqual(a.Search, b.Search)) return false;

        return true;
    }

    private bool AreBackTargetsEqual(BackTarget? a, BackTarget? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        // Note: Action functions are compared by reference equality.
        // This means passing a new lambda with the same logic will be treated as different.
        // This is intentional to keep the implementation simple and avoid complex equality checks.
        return a.Visible == b.Visible &&
               a.Uri == b.Uri &&
               ReferenceEquals(a.Action, b.Action);
    }

    private bool AreSearchActionsEqual(SearchAction? a, SearchAction? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        // Note: Action functions are compared by reference equality.
        // This means passing a new lambda with the same logic will be treated as different.
        // This is intentional to keep the implementation simple and avoid complex equality checks.
        return a.Uri == b.Uri &&
               ReferenceEquals(a.Action, b.Action);
    }

    private BottomNavState CreateDefaultState()
    {
        return CreateState(null);
    }

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

        // Search button: always visible, uses custom action/URI or default
        NavButton search;
        if (options?.Search?.Action is not null)
        {
            search = new(
                Visible: true,
                Enabled: true,
                OnClick: options.Search.Action
            );
        }
        else if (options?.Search?.Uri is not null)
        {
            string searchUri = options.Search.Uri;
            search = new(
                Visible: true,
                Enabled: true,
                OnClick: () =>
                {
                    navigationManager.NavigateTo(searchUri);
                    return Task.CompletedTask;
                }
            );
        }
        else
        {
            // Default search behavior
            search = new(
                Visible: true,
                Enabled: true,
                OnClick: () =>
                {
                    navigationManager.NavigateTo(defaults.SearchUri);
                    return Task.CompletedTask;
                }
            );
        }

        // Back button: visible only if configured, executes action or navigates to URI
        NavButton back;
        if (options?.Back is { Visible: true })
        {
            if (options.Back.Action is not null)
            {
                back = new(
                    Visible: true,
                    Enabled: true,
                    OnClick: options.Back.Action
                );
            }
            else if (options.Back.Uri is not null)
            {
                string backUri = options.Back.Uri;
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
                // Back button configured as visible but no action/URI provided - disabled state
                back = new(
                    Visible: true,
                    Enabled: false,
                    OnClick: () => Task.CompletedTask
                );
            }
        }
        else
        {
            // Back button not visible by default
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

    public void Dispose()
    {
        navigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }
}
