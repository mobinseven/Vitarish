namespace Vitarish.Modules.Base.Interface.Services;

[RegisterScoped]
public sealed class NavService : INavService, IDisposable
{

    private readonly IRenderNotifier renderNotifier;
    private readonly IStringLocalizer localizer;
    private readonly NavigationManager navigationManager;
    private readonly NavServiceRoutes routes;
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
    public event EventHandler<TitleChangeEventArgs>? TitleChanged;

    public NavService(
        NavigationManager navigationManager,
        IStringLocalizer localizer,
        IRenderNotifier renderNotifier,
        NavServiceRoutes routes)
    {
        this.renderNotifier = renderNotifier;
        this.localizer = localizer;
        this.navigationManager = navigationManager;
        this.routes = routes;
        GenerateCurrentBreadcrumbs();
        navigationManager.LocationChanged += HandleLocationChanged;
    }

    public void OverridePageTitle(string id, string title)
    {
        Breadcrumb? bc = breadcrumbs?.FirstOrDefault(b => b.Id == id);
        if (bc is null)
        {
            return;
        }
        bc.Text = title;
        TitleChanged?.Invoke(this, new TitleChangeEventArgs(title));
        _ = renderNotifier.Notify();
    }

    public void EnableSearch() => _ = renderNotifier.Notify();

    public void DisableSearch()
    {
        if (SearchString is not null)
        {
            SearchString = null;
        }
    }

    private void GenerateCurrentBreadcrumbs(bool force = false)
    {
        if (breadcrumbs is not null && !force)
        {
            return;
        }
        string[] subs = CurrentLocation.Split('/');
        breadcrumbs = [new Breadcrumb { Id = "/", Path = "/", Text = localizer["Dashboard"] }];
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
                breadcrumbs.Add(new Breadcrumb { Path = path, Text = sub, Id = sub });
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
    public void Profile() => navigationManager.NavigateTo(routes.ProfileRoute);
}


public class TitleChangeEventArgs(string Title) : EventArgs
{
    public string Title { get; } = Title;
}
