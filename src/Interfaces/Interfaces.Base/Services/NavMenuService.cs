namespace Vitarish.Interfaces.Base.Services;

/// <summary>
/// Provides centralized configuration for <see cref="Vitarish.Interfaces.Base.Common.NavMenu" /> buttons.
/// </summary>
[RegisterScoped]
public sealed class NavMenuService : INavMenuService, IDisposable
{
    private NavMenuState state = NavMenuState.Default;

    public NavMenuService(NavigationManager navigationManager)
    {
        NavigationManager = navigationManager;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    public event EventHandler? StateChanged;

    public NavigationManager NavigationManager { get; }

    public NavMenuState State => state;

    public void Configure(string? homeUri = null, NavMenuAction? backAction = null, RenderFragment? additionalButtons = null) =>
        SetState(new NavMenuState(homeUri, backAction, additionalButtons));

    public void Configure(NavMenuState newState) =>
        SetState(newState);

    public void Reset() =>
        SetState(NavMenuState.Default);

    private void SetState(NavMenuState newState)
    {
        NavMenuState normalizedState = newState.Normalize();

        if (normalizedState == state)
        {
            return;
        }

        state = normalizedState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) =>
        Reset();

    public void Dispose() =>
        NavigationManager.LocationChanged -= HandleLocationChanged;
}

public interface INavMenuService
{
    /// <summary>
    /// Raised when the navigation menu configuration changes.
    /// </summary>
    event EventHandler? StateChanged;

    NavMenuState State { get; }

    void Configure(string? homeUri = null, NavMenuAction? backAction = null, RenderFragment? additionalButtons = null);

    void Configure(NavMenuState newState);

    void Reset();
}

/// <summary>
/// Captures the current navigation menu configuration.
/// </summary>
public sealed record NavMenuState(string? HomeUri, NavMenuAction? BackAction, RenderFragment? AdditionalButtons)
{
    public static NavMenuState Default { get; } = new("/", null, null);

    public NavMenuState Normalize() =>
        string.IsNullOrWhiteSpace(HomeUri)
            ? this with { HomeUri = "/" }
            : this;
}
