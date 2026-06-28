using Vitarish.Modules.Base.Interface.Abstractions;
using Vitarish.Modules.Base.Interface.Models;

namespace Vitarish.Main.Components;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;
    [Inject] protected IPlatformThemePaletteProvider PlatformThemePaletteProvider { get; set; } = default!;
    [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected bool Initialized;
    protected Breakpoint PageBreakpoint
    {
        get;
        set
        {
            field = value;
            StateHasChanged();
        }
    }
    protected bool BreakpointReceived
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
            try
            {
                HttpClient client = HttpClientFactory.CreateClient();
                string? theme = await client.GetStringAsync($"/api/modules/users/{userId}/theme");
                themeSettings = ThemeSettings.FromJson(theme);
            }
            catch
            {
                // Theme loading is best-effort
            }
        }
        PlatformThemePalette? platformPalette = await PlatformThemePaletteProvider.GetActivePaletteAsync();
        await ThemeService.InitializeAsync(themeSettings, platformPalette);
        Initialized = true;
        StateHasChanged();
    }
}
