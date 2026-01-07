namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private INavMenuService NavMenuService { get; set; } = default!;

    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool searchActive;
    private string? query;

    private NavMenuAction? BackAction => NavMenuService.State.BackAction;

    private string? BackHref => BackAction?.OnClick is null ? BackAction?.Href : null;

    private string HomeHref => NavMenuService.State.HomeUri ?? "/";

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        NavMenuService.StateChanged += OnNavMenuStateChanged;
    }

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

    private Task HandleBackClick() => BackAction?.OnClick is null ? Task.CompletedTask : BackAction.InvokeAsync(NavigationManager);

    private async void OnNavMenuStateChanged(object? sender, EventArgs e) =>
        await InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        NavMenuService.StateChanged -= OnNavMenuStateChanged;
        GC.SuppressFinalize(this);
    }
}
