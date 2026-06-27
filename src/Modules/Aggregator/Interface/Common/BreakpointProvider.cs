namespace Vitarish.Modules.Aggregator.Interface.Common;

public class BreakpointProvider : ComponentBase, IBrowserViewportObserver, IAsyncDisposable
{
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;
    [Parameter]
    public Breakpoint Breakpoint { get; set; }
    [Parameter]
    public EventCallback<Breakpoint> BreakpointChanged { get; set; }
    [Parameter]
    public EventCallback BreakpointReceived { get; set; }
    public Guid Id { get; } = Guid.NewGuid();
    private readonly List<Breakpoint> breakpointHistory = [];
    private Breakpoint? startingBreakpoint;


    async Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        if (browserViewportEventArgs.IsImmediate)
        {
            startingBreakpoint = browserViewportEventArgs.Breakpoint;
            await BreakpointReceived.InvokeAsync();
        }
        else
        {
            breakpointHistory.Add(browserViewportEventArgs.Breakpoint);
        }
        Breakpoint = browserViewportEventArgs.Breakpoint;
        await BreakpointChanged.InvokeAsync(Breakpoint);
        await InvokeAsync(StateHasChanged);
    }


    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        ReportRate = 250,
        NotifyOnBreakpointOnly = true
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);

        GC.SuppressFinalize(this);
    }
}
