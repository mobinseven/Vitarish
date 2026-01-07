namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBottomNavService BottomNavService { get; set; } = default!;
    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool searchActive;
    private string? query
    {
        get;
        set
        {
            field = value;
            SearchService.Search(this, value);
        }
    }
    private BottomNavState State => BottomNavService.Current;
    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void Close()
    {
        searchActive = false;
        SearchService.Search(this, null);
    }

    private void Open()
    {
        searchActive = false;
        SearchService.Search(this, query);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Close();
        await InvokeAsync(StateHasChanged);
    }


    private async void OnNavMenuStateChanged(object? sender, EventArgs e) =>
        await InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }

    private Task HandleHomeClick() => HandleNavButtonClick(State.Home.OnClick);
    private Task HandleBackClick() => HandleNavButtonClick(State.Back.OnClick);
    private Task HandleProfileClick() => HandleNavButtonClick(State.Profile.OnClick);
    private static async Task HandleNavButtonClick(Func<Task> onClick) => await onClick();

}
