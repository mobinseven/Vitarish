using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Reflection;
using Vitarish.Interfaces.Base.Attributes;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Interfaces.Aggregator.Components;


public partial class NavBar : CommonBase, IAsyncDisposable
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

    protected bool SearchActive => NavService.SearchString is not null;

    protected bool ChatActive;
    protected override void OnInitialized()
    {
        Search = RouteData?.PageType.GetCustomAttribute<EnableSearchAttribute>(inherit: true) is not null;
        ChatActive = RouteData?.PageType.GetCustomAttribute<EnableChatAttribute>(inherit: true) is not null;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    protected async void CloseSearch()
    {
        NavService.SearchString = null;
        await InvokeAsync(StateHasChanged);
    }

    protected async void OpenSearch()
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
        ChatActive = RouteData?.PageType.GetCustomAttribute<EnableChatAttribute>(inherit: true) is not null;
        Search = RouteData?.PageType.GetCustomAttribute<EnableSearchAttribute>(inherit: true) is not null;
        CloseSearch();
    }
    protected void HandleHomeClick() => NavService.Home();
    protected void HandleBackClick() => NavService.Back();
    protected void HandleProfileClick() => NavService.Profile();

    private IScrollListener? _scrollListener;
    protected bool ScrollToTopVisible;
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
    protected async Task OnScrollToTopClick(MouseEventArgs args) => await ScrollManager.ScrollToTopAsync(_scrollListener?.Selector, ScrollBehavior.Smooth);
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
