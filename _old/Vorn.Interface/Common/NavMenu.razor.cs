using Vorn.Interface.Services;

namespace Vorn.Interface.Common;


public partial class NavMenu : IDisposable
{
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    [Parameter] public Size Size { get; set; } = Size.Large;

    bool searchActive;
    string? query;

    protected override void OnInitialized() =>
        NavigationManager.LocationChanged += OnLocationChanged;

    void Search() => SearchService.Search(this, query);

    void Close()
    {
        searchActive = false;
        SearchService.Search(this, null);
    }

    async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Close();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
}
