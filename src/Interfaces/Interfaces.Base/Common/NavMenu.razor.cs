using Microsoft.AspNetCore.Components.Routing;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

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
            NavigateSearch();
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
        catch
        {
            // Intentionally ignore to avoid surfacing navigation callback exceptions.
        }
    }


    private async void OnNavMenuStateChanged()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Rendering failures are surfaced via Blazor; suppress here to avoid unobserved tasks.
        }
    }

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

    private void NavigateSearch()
    {
        if (!State.Search.Enabled)
        {
            return;
        }

        string searchUri = BottomNavService.GetSearchUri(query);
        BottomNavService.UpdateSearchUri(searchUri);
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
}
