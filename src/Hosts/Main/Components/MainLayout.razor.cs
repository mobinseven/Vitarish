using Vitarish.Interfaces.Accounts.Abstractions;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Main.Components;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;

    private bool Initialized;
    protected Breakpoint PageBreakpoint
    {
        get;
        set
        {
            field = value;
            StateHasChanged();
        }
    }
    private bool BreakpointReceived
    {
        get;
        set
        {
            field = value;
            StateHasChanged();
        }
    }

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

    private async void RenderNotificationReceived(object? sender, RenderNotification notification)
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopRight;
        _ = Snackbar.Add<MudProgressCircular>(new Dictionary<string, object>() { { "Indeterminate", true } }, configure: c => { c.SnackbarTypeClass = "glass"; c.SnackbarVariant = Variant.Filled; c.DuplicatesBehavior = SnackbarDuplicatesBehavior.Prevent; c.BackgroundBlurred = true; c.MaximumOpacity = 100; c.HideIcon = true; c.ShowCloseIcon = false; c.VisibleStateDuration = 500; c.ShowTransitionDuration = 200; c.HideTransitionDuration = 200; }, key: nameof(MainLayout));
    }
}
