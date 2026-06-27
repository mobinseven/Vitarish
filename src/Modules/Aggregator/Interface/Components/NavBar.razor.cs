using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Reflection;
using Vitarish.Modules.Base.Interface.Attributes;
using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Modules.Base.Interface.Models;

namespace Vitarish.Modules.Aggregator.Interface.Components;


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
    protected bool ChatWaiting;
    protected RenderFragment? ChatContext;

    protected override void OnInitialized()
    {
        Search = RouteData?.PageType.GetCustomAttribute<EnableSearchAttribute>(inherit: true) is not null;
        ChatActive = RouteData?.PageType.GetCustomAttribute<EnableChatAttribute>(inherit: true) is not null;
        NavigationManager.LocationChanged += OnLocationChanged;
        ChatService.OnChatWait += OnChatWait;
        ChatService.OnChatContext += OnChatContext;
    }

    private async void OnChatContext(object? sender, ChatContextEventArgs e)
    {
        ChatContext = e.ChatContext;
        await InvokeAsync(StateHasChanged);
    }

    private async void OnChatWait(object? sender, ChatWaitEventArgs e)
    {
        ChatWaiting = e.Waiting;
        await InvokeAsync(StateHasChanged);
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
    protected bool ScrollToBotVisible;
    private readonly int TopOffset = 200;
    private readonly int BotOffset = 200;

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

        double? botOffset = e.NodeName == "#document"
            ? e.FirstChildBoundingClientRect?.Bottom - e.ClientHeight
            : e.ScrollHeight - e.ClientHeight - e.ScrollTop;

        var shouldShowTop = topOffset >= TopOffset;
        var shouldShowBot = botOffset >= BotOffset;

        var changed = false;

        if (ScrollToTopVisible != shouldShowTop)
        {
            ScrollToTopVisible = shouldShowTop;
            changed = true;
        }

        if (ScrollToBotVisible != shouldShowBot)
        {
            ScrollToBotVisible = shouldShowBot;
            changed = true;
        }

        if (changed)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    protected async Task OnScrollToTopClick(MouseEventArgs args) => await ScrollManager.ScrollToTopAsync(_scrollListener?.Selector, ScrollBehavior.Smooth);
    protected async Task OnScrollToBotClick(MouseEventArgs args) => await ScrollManager.ScrollToBottomAsync(_scrollListener?.Selector, ScrollBehavior.Smooth);
    public async ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        ChatService.OnChatWait -= OnChatWait;
        ChatService.OnChatContext -= OnChatContext;
        _scrollListener?.OnScroll -= ScrollListenerOnScroll;
        if (_scrollListener is not null)
        {
            await _scrollListener.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
