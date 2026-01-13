using Vitarish.Interfaces.Base.Abstractions;

namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private INavService NavService { get; set; } = default!;
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
            NavigateSearch();
        }
    }
    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        SyncQueryFromUri(NavigationManager.Uri);
    }

    private void Close()
    {
        searchActive = false;
        if (!string.IsNullOrEmpty(query))
        {
            query = null;
            NavigateSearch();
        }
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            searchActive = false;
            SyncQueryFromUri(e.Location);
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    private void HandleSearchClick() => searchActive = true;

    private void NavigateSearch()
    {
        string searchUri = NavService.GetSearchUri(query);
        NavigationManager.NavigateTo(searchUri);
    }

    private void SyncQueryFromUri(string uri)
    {
        string? currentQuery = SearchQueryHelper.ExtractQuery(NavigationManager, uri);

        if (!string.Equals(query, currentQuery, StringComparison.Ordinal))
        {
            query = currentQuery;
        }

        searchActive = !string.IsNullOrEmpty(query);
    }

    private void HandleHomeClick() => NavService.Home();
    private void HandleBackClick() => NavService.Back();
    private void HandleProfileClick() => NavService.Profile();


    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
