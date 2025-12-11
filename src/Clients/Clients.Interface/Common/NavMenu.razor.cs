using Vitarish.Clients.Interface.Services;

namespace Vitarish.Clients.Interface.Common;


public partial class NavMenu : IDisposable
{
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool searchActive;
    private string? query;

    protected override void OnInitialized() =>
        NavigationManager.LocationChanged += OnLocationChanged;

    private void Search() => SearchService.Search(this, query);

    private void Close()
    {
        searchActive = false;
        SearchService.Search(this, null);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Close();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
}
