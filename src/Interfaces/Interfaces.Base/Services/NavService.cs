using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
public sealed class NavService : INavService, IDisposable
{

    private readonly IRenderNotifier renderNotifier;
    private readonly IStringLocalizer localizer;
    private readonly NavigationManager navigationManager;
    private List<Breadcrumb>? breadcrumbs;
    private string CurrentLocation => navigationManager.ToBaseRelativePath(navigationManager.Uri.RemoveAllQueryParams()).TrimEnd('/');
    public Breadcrumb[] Breadcrumbs => breadcrumbs?.ToArray() ?? [];
    public string? SearchString
    {
        get => field;
        set
        {
            field = value;
            if (value == string.Empty)
            {
                return;
            }

            _ = renderNotifier.Notify(new SearchArgs(value));
        }
    }

    public NavService(NavigationManager navigationManager, IStringLocalizer localizer, IRenderNotifier renderNotifier)
    {
        this.renderNotifier = renderNotifier;
        this.localizer = localizer;
        this.navigationManager = navigationManager;
        GenerateCurrentBreadcrumbs();
        navigationManager.LocationChanged += HandleLocationChanged;
    }

    public void OverridePageTitle(string id, string text)
    {
        Breadcrumb? bc = breadcrumbs?.FirstOrDefault(b => b.Id == id);
        if (bc is null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(bc.Text))
        {
            return;
        }
        bc.Text = text;
        _ = renderNotifier.Notify();
    }

    public void EnableSearch()
    {
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(nameof(NavMenu.Search), true));
        _ = renderNotifier.Notify();
    }

    private void GenerateCurrentBreadcrumbs(bool force = false)
    {
        if (breadcrumbs is not null && !force)
        {
            return;
        }
        string[] subs = CurrentLocation.Split('/');
        breadcrumbs = [new Breadcrumb { Path = AppRoutes.Home, Text = localizer["Dashboard"] }];
        string path = string.Empty;
        foreach (string sub in subs)
        {
            if (string.IsNullOrWhiteSpace(sub))
            {
                continue;
            }

            path += sub + "/";
            if (localizer[sub].ResourceNotFound)
            {
                breadcrumbs.Add(new Breadcrumb { Path = path, Id = sub });
            }
            else
            {
                breadcrumbs.Add(new Breadcrumb { Path = path, Text = localizer[sub], Id = sub });
            }
        }
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        GenerateCurrentBreadcrumbs(true);
        _ = renderNotifier.Notify();
    }

    private void NavigateUpOneLevel()
    {
        string absolutePath = new Uri(navigationManager.Uri).AbsolutePath;
        string normalizedPath = absolutePath.EndsWith("/")
            ? absolutePath.TrimEnd('/')
            : absolutePath;

        if (string.IsNullOrEmpty(normalizedPath) || normalizedPath == AppRoutes.Home)
        {
            navigationManager.NavigateTo(AppRoutes.Home);
        }

        int lastSlashIndex = normalizedPath.LastIndexOf('/');
        string parentPath = lastSlashIndex <= 0 ? AppRoutes.Home : normalizedPath[..lastSlashIndex];
        navigationManager.NavigateTo(parentPath);
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    public void Back() => NavigateUpOneLevel();
    public void Home() => navigationManager.NavigateTo(AppRoutes.Home);
    public void Profile() => navigationManager.NavigateTo(AppRoutes.Account.Settings);
}
