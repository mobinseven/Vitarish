using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Interfaces.Base.Services;

namespace Vitarish.Main.Components;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable, IBrowserViewportObserver, IRenderNotificationReceiver
{
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] private ILayoutService LayoutService { get; set; } = default!;
    [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IStringLocalizer Localizer { get; set; } = default!;

    private bool Initialized;
    private bool Navigating;
    private IDisposable? subscription;
    protected Breakpoint PageBreakpoint { get; set; }
    private readonly List<Breakpoint> breakpointHistory = [];
    private Breakpoint? startingBreakpoint;
    private bool BreakpointReceived => startingBreakpoint is not null;

    protected override async Task OnInitializedAsync()
    {
        ThemeSettings? themeSettings = null;
        string? userId = HttpContextAccessor.HttpContext?.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            string? theme = await UserService.GetThemeAsync(userId);
            themeSettings = ThemeSettings.FromJson(theme);
        }
        await ThemeService.InitializeAsync(themeSettings);
        Initialized = true;
        LayoutService.SearchStateChanged += OnSearchStateChanged;
        subscription = RenderNotifier.Subscribe(this);
        NavService.RenderRequired += HandleLocationChanged;
        _ = NavigationManager.RegisterLocationChangingHandler(async c => { Navigating = true; await InvokeAsync(() => StateHasChanged()); });
        StateHasChanged();
    }


    private void HandleLocationChanged(object? sender, EventArgs e)
    {
        Navigating = false;
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
        NavService.RenderRequired -= HandleLocationChanged;
        LayoutService.SearchStateChanged -= OnSearchStateChanged;
        await BrowserViewportService.UnsubscribeAsync(this);
        subscription?.Dispose();

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

    public void OnCompleted() => throw new NotImplementedException();
    public void OnError(Exception error) => throw new NotImplementedException();
    void IObserver<RenderNotification>.OnNext(RenderNotification value) => RenderNotificationReceived(this, value);

    private void OnSearchStateChanged(object? sender, EventArgs e)
        => _ = InvokeAsync(StateHasChanged);
}
