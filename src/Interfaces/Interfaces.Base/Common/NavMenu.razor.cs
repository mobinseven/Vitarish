using Microsoft.AspNetCore.Components.Web;
using System.Reflection;
using Vitarish.Main.Components;

namespace Vitarish.Interfaces.Base.Common;


public partial class NavMenu : CommonBase, IAsyncDisposable
{
    [CascadingParameter(Name = "CurrentRouteData")]
    public RouteData? RouteData { get; set; }

    [Inject]
    private IScrollListenerFactory ScrollListenerFactory { get; set; } = null!;

    [SupplyParameterFromQuery]
    [Parameter]
    public bool Search { get; set; }

    [Parameter] public EventCallback RedirectToSearch { get; set; }
    [Parameter] public Size Size { get; set; } = Size.Large;

    private bool SearchActive => NavService.SearchString is not null;
    protected override void OnInitialized()
    {
        Search = RouteData?.PageType.GetCustomAttribute<EnableSearchAttribute>(inherit: true) is not null;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private async void Close()
    {
        NavService.SearchString = null;
        await InvokeAsync(StateHasChanged);
    }

    private async void Open()
    {
        if (Search)
        {
            NavService.SearchString = string.Empty;
            await InvokeAsync(StateHasChanged);

        }
        else
        {
            await RedirectToSearch.InvokeAsync();
        }
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {

        Search = RouteData?.PageType.GetCustomAttribute<EnableSearchAttribute>(inherit: true) is not null;
        Close();
    }
    private void HandleHomeClick() => NavService.Home();
    private void HandleBackClick() => NavService.Back();
    private void HandleProfileClick() => NavService.Profile();

    private IScrollListener? _scrollListener;
    private bool ScrollToTopVisible;
    private readonly int TopOffset = 200;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _scrollListener = ScrollListenerFactory.Create(null);

            //subscribe to event
            _scrollListener.OnScroll += ScrollListenerOnScroll;
        }
    }
    private async void ScrollListenerOnScroll(object? sender, ScrollEventArgs e)
    {
        double? topOffset = e.NodeName == "#document"
            ? e.FirstChildBoundingClientRect?.Top * -1
            : e.ScrollTop;

        if (topOffset >= TopOffset && ScrollToTopVisible != true)
        {
            ScrollToTopVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        if (topOffset < TopOffset && ScrollToTopVisible)
        {
            ScrollToTopVisible = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    private async Task OnScrollToTopClick(MouseEventArgs args) => await ScrollManager.ScrollToTopAsync(_scrollListener?.Selector, ScrollBehavior.Smooth);
    public async ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        _scrollListener?.OnScroll -= ScrollListenerOnScroll;
        if (_scrollListener is not null)
        {
            await _scrollListener.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
