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
            renderNotifier.Notify();
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
        _ = (bc?.Text = text);
        renderNotifier.Notify();
    }

    public void EnableSearch()
    {
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(nameof(NavMenu.Search), true));
        renderNotifier.Notify();
    }

    private void GenerateCurrentBreadcrumbs()
    {
        string[] subs = CurrentLocation.Split('/');
        breadcrumbs = [new Breadcrumb { Path = "/", Text = localizer["Dashboard"] }];
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
        GenerateCurrentBreadcrumbs();
        renderNotifier.Notify();
    }

    private void NavigateUpOneLevel()
    {
        string absolutePath = new Uri(navigationManager.Uri).AbsolutePath;
        string normalizedPath = absolutePath.EndsWith("/")
            ? absolutePath.TrimEnd('/')
            : absolutePath;

        if (string.IsNullOrEmpty(normalizedPath) || normalizedPath == "/")
        {
            navigationManager.NavigateTo("/");
        }

        int lastSlashIndex = normalizedPath.LastIndexOf('/');
        string parentPath = lastSlashIndex <= 0 ? "/" : normalizedPath[..lastSlashIndex];
        navigationManager.NavigateTo(parentPath);
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    public void Back() => NavigateUpOneLevel();
    public void Home() => navigationManager.NavigateTo("/");
    public void Profile() => navigationManager.NavigateTo("/Settings");
}
