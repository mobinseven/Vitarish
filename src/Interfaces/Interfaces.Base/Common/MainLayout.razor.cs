namespace Vitarish.Interfaces.Base.Common;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable, IBrowserViewportObserver, IRenderNotificationReceiver
{
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private ILayoutService LayoutService { get; set; } = default!;

    private bool Initialized;
    private bool darkmood;

    protected MudTheme Theme => ThemeService.Theme;
    protected ThemeSurface CurrentThemeSurface => ThemeService.GetSurface(darkmood);

    private IDisposable? subscription;
    protected Breakpoint PageBreakpoint { get; set; }
    private readonly List<Breakpoint> breakpointHistory = [];
    private Breakpoint? startingBreakpoint;
    private bool BreakpointReceived => startingBreakpoint is not null;

    protected override void OnInitialized()
    {
        Initialized = true;
        darkmood = ThemeService.DarkModeEnabled;
        ThemeService.ThemeModified += OnThemeModified;
        LayoutService.ThemeStateChanged += OnThemeStateChanged;
        LayoutService.SearchStateChanged += OnSearchStateChanged;
        subscription = RenderNotifier.Subscribe(this);
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);

            //if (ThemeService.BlazorReadyEnabled)
            //{
            //    await JS.InvokeVoidAsync("eval", "if(blazorReady) blazorReady();");
            //}
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
        ThemeService.ThemeModified -= OnThemeModified;
        LayoutService.ThemeStateChanged -= OnThemeStateChanged;
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

    private void OnThemeModified(object? sender, EventArgs e) => HandleThemeChanged();

    private void OnThemeStateChanged(object? sender, EventArgs e) => HandleThemeChanged();

    private void HandleThemeChanged()
    {
        darkmood = ThemeService.DarkModeEnabled;
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnSearchStateChanged(object? sender, EventArgs e)
        => _ = InvokeAsync(StateHasChanged);
}
