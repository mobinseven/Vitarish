using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Main.Components;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable, IBrowserViewportObserver
{
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;

    private bool Initialized;
    protected Breakpoint PageBreakpoint { get; set; }
    private readonly List<Breakpoint> breakpointHistory = [];
    private Breakpoint? startingBreakpoint;
    private bool BreakpointReceived => startingBreakpoint is not null;

    protected override async Task OnInitializedAsync()
    {
        ThemeSettings? themeSettings = null;
        string? userId = RequestContext.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            string? theme = await UserService.GetThemeAsync(userId);
            themeSettings = ThemeSettings.FromJson(theme);
        }
        await ThemeService.InitializeAsync(themeSettings);
        Initialized = true;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async void RenderNotificationReceived(object? sender, RenderNotification notification)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopRight;
        _ = Snackbar.Add<MudProgressCircular>(new Dictionary<string, object>() { { "Indeterminate", true } }, configure: c => { c.SnackbarTypeClass = "glass"; c.SnackbarVariant = Variant.Filled; c.DuplicatesBehavior = SnackbarDuplicatesBehavior.Prevent; c.BackgroundBlurred = true; c.MaximumOpacity = 100; c.HideIcon = true; c.ShowCloseIcon = false; c.VisibleStateDuration = 500; c.ShowTransitionDuration = 200; c.HideTransitionDuration = 200; }, key: nameof(MainLayout));
    }

    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);

        GC.SuppressFinalize(this);
    }

    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        ReportRate = 250,
        NotifyOnBreakpointOnly = true
    };

    public Guid Id { get; private set; } = Guid.NewGuid();

    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        if (browserViewportEventArgs.IsImmediate)
        {
            startingBreakpoint = browserViewportEventArgs.Breakpoint;
        }
        else
        {
            breakpointHistory.Add(browserViewportEventArgs.Breakpoint);
        }
        PageBreakpoint = browserViewportEventArgs.Breakpoint;
        return InvokeAsync(StateHasChanged);
    }
}
