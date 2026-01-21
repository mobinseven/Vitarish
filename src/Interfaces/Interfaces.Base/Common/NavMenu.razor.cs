namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : CommonBase, IDisposable
{
    [SupplyParameterFromQuery]
    [Parameter]
    public bool Search { get; set; }
    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool SearchActive => NavService.SearchString is not null;
    protected override void OnInitialized() => NavigationManager.LocationChanged += OnLocationChanged;

    private async void Close()
    {
        NavService.SearchString = null;
        await InvokeAsync(StateHasChanged);
    }

    private async void Open()
    {
        NavService.SearchString = string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Close();
    private void HandleHomeClick() => NavService.Home();
    private void HandleBackClick() => NavService.Back();
    private void HandleProfileClick() => NavService.Profile();


    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
