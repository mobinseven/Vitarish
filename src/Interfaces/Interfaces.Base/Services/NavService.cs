using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Constants;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

/// <summary>
/// Implementation of the bottom navigation service.
/// </summary>
[RegisterScoped]
public sealed class NavService : INavService, IDisposable
{
    private readonly NavigationManager navigationManager;
    private readonly NavDefaults defaults;
    private NavOptions? currentOptions;

    public NavService(
        NavigationManager navigationManager,
        NavDefaults? defaults = null)
    {
        this.navigationManager = navigationManager;
        this.defaults = defaults ?? NavDefaults.Default;
        Current = CreateDefaultState();
        currentOptions = null;

        navigationManager.LocationChanged += HandleLocationChanged;
    }

    public event Action? Changed;

    public BottomNavState Current { get; private set; }

    public void Apply(NavOptions? options)
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

    private static bool AreOptionsEqual(NavOptions? a, NavOptions? b)
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

        return a is not null && b is not null && a.Visible == b.Visible &&
               a.Enabled == b.Enabled &&
               a.Uri == b.Uri &&
               ReferenceEquals(a.Action, b.Action);
    }

    private BottomNavState CreateDefaultState() => CreateState(null);

    private BottomNavState CreateState(NavOptions? options)
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
        else
        {
            back = options?.Back is { Visible: false }
                ? new(
                Visible: false,
                Enabled: false,
                OnClick: NavigateUpOneLevelAsync
            )
                : new(
                Visible: true,
                Enabled: true,
                OnClick: NavigateUpOneLevelAsync
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
        NavOptions merged = currentOptions is null
            ? new NavOptions(SearchUriOverride: searchUri)
            : currentOptions with
            {
                Search = null,
                SearchUriOverride = searchUri
            };

        Apply(merged);
    }

    private NavButton CreateSearchButton(NavOptions? options)
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
