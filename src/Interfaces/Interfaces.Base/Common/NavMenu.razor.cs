using Microsoft.AspNetCore.Components.Routing;

namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private INavService BottomNavService { get; set; } = default!;
    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool searchActive;
    private string? query;
    private string? Query
    {
        get => query;
        set
        {
            if (string.Equals(query, value, StringComparison.Ordinal))
            {
                return;
            }

            query = value;
            _ = NavigateSearchAsync();
        }
    }
    private BottomNavState State => BottomNavService.Current;
    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        BottomNavService.Changed += OnNavMenuStateChanged;
        SyncQueryFromUri(NavigationManager.Uri);
    }

    private void Close()
    {
        searchActive = false;
        if (!string.IsNullOrEmpty(query))
        {
            query = null;
            _ = NavigateSearchAsync();
        }
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        searchActive = false;
        SyncQueryFromUri(e.Location);
        await InvokeAsync(StateHasChanged);
    }


    private async void OnNavMenuStateChanged() => await InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        BottomNavService.Changed -= OnNavMenuStateChanged;
        GC.SuppressFinalize(this);
    }

    private Task HandleHomeClick() => HandleNavButtonClick(State.Home.OnClick);
    private Task HandleBackClick() => HandleNavButtonClick(State.Back.OnClick);
    private Task HandleProfileClick() => HandleNavButtonClick(State.Profile.OnClick);
    private static async Task HandleNavButtonClick(Func<Task> onClick) => await onClick();

    private void HandleSearchClick() => searchActive = true;

    private async Task NavigateSearchAsync()
    {
        if (!State.Search.Enabled)
        {
            return;
        }

        string searchUri = BottomNavService.GetSearchUri(query);
        BottomNavService.UpdateSearchUri(searchUri);
        await HandleNavButtonClick(BottomNavService.Current.Search.OnClick);
    }

    private void SyncQueryFromUri(string uri)
    {
        string? currentQuery = ExtractQuery(uri);

        if (!string.Equals(query, currentQuery, StringComparison.Ordinal))
        {
            query = currentQuery;
        }

        searchActive = !string.IsNullOrEmpty(query);
    }

    private string? ExtractQuery(string uri)
    {
        Uri absolute = NavigationManager.ToAbsoluteUri(uri);
        string queryString = absolute.Query;
        if (string.IsNullOrEmpty(queryString) || queryString.Length <= 1)
        {
            return null;
        }

        foreach (string segment in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            if (!string.Equals(segment[..separatorIndex], "q", StringComparison.Ordinal))
            {
                continue;
            }

            string value = segment[(separatorIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }

        return null;
    }
}
