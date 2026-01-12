using System.Security;
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
    private readonly IStringLocalizer localizer;
    private readonly NavigationManager navigationManager;
    private readonly NavDefaults defaults;
    private BottomNavOptions? currentOptions;
    public Stack<Breadcrumb> Breadcrumbs { get; private set; } = [];
    private string breadcrumbsLocation;
    private string currentLocation = string.Empty;
    public event EventHandler? RenderRequired;
    public NavService(
        NavigationManager navigationManager,
        IStringLocalizer localizer,
        NavDefaults? defaults = null)
    {
        this.localizer = localizer;
        this.navigationManager = navigationManager;
        this.defaults = defaults ?? NavDefaults.Default;
        CurrentBottomNavState = CreateDefaultBottomNavState();
        currentOptions = null;
        currentLocation = GetCurrentPath();
        GenerateCurrentBreadcrumbs();
        navigationManager.LocationChanged += HandleLocationChanged;
    }

    private string GetCurrentPath() => navigationManager.ToBaseRelativePath(navigationManager.Uri.RemoveAllQueryParams()).TrimEnd('/');

    public void OverridePageTitle(string text, string title)
    {
        Breadcrumb? bc = Breadcrumbs.FirstOrDefault(b => b.Text.Contains(text));
        _ = (bc?.Text = title);
        RenderRequired?.Invoke(this, new EventArgs());
    }

    private void GenerateCurrentBreadcrumbs()
    {
        string[] subs = currentLocation.Split('/');
        if (subs.Length < Breadcrumbs.Count - 1)
        {
            _ = Breadcrumbs.Pop();
        }
        else if (breadcrumbsLocation != currentLocation)
        {
            Breadcrumbs.Clear();
            string path = string.Empty;
            Breadcrumbs.Push(new Breadcrumb("/", localizer["Dashboard"]));
            foreach (string sub in subs)
            {
                if (string.IsNullOrEmpty(sub))
                {
                    continue;
                }
                path += sub + "/";
                Breadcrumbs.Push(new Breadcrumb(path, localizer[sub]));
            }
        }
        breadcrumbsLocation = currentLocation;
    }

    public event Action? Changed;

    public BottomNavState CurrentBottomNavState { get; private set; }

    public void Apply(BottomNavOptions? options)
    {
        if (AreOptionsEqual(currentOptions, options))
        {
            return;
        }

        currentOptions = options;
        CurrentBottomNavState = CreateState(options);
        Changed?.Invoke();
    }

    public void ResetNavToDefaults() => Apply(null);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentLocation = GetCurrentPath();
        GenerateCurrentBreadcrumbs();
        ResetNavToDefaults();
        RenderRequired?.Invoke(this, new EventArgs());
    }

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

    private static bool AreSearchActionsEqual(SearchAction? a, SearchAction? b) => ReferenceEquals(a, b) || !(a is null || b is null || a.Visible != b.Visible ||
               a.Enabled != b.Enabled ||
               a.Uri != b.Uri ||
               !ReferenceEquals(a.Action, b.Action));

    private BottomNavState CreateDefaultBottomNavState() => CreateState(null);

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

        return new BottomNavState(home, back, profile);
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
        string searchUri = string.IsNullOrWhiteSpace(query) ? navigationManager.Uri.RemoveAllQueryParams() : navigationManager.GetUriWithQueryParameter("q", query);
        return searchUri;
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

}
