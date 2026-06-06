using Vitarish.Interfaces.Admins.Components;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Models;
using Vitarish.Platform.Interface.Components;

namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminThemeEditPage : PlatformAdminCommonBase
{
    [Parameter] public Guid? ThemeId { get; set; }
    [Inject] private IPlatformThemeQuery ThemeQuery { get; set; } = default!;
    [Inject] private IPlatformThemeCommand ThemeCommand { get; set; } = default!;

    private PlatformThemeForm.PlatformThemeFormModel FormModel { get; set; } = new();
    private Guid? loadedThemeId;

    protected override async Task OnParametersSetAsync()
    {
        if (ThemeId == loadedThemeId)
        {
            return;
        }

        loadedThemeId = ThemeId;

        if (ThemeId is null)
        {
            FormModel = new();
            return;
        }

        ThemeEditorModel? theme = await ThemeQuery.GetThemeAsync(ThemeId.Value);
        if (theme is null)
        {
            Cancel();
            return;
        }

        FormModel = ToFormModel(theme);
    }

    private async Task Save(PlatformThemeForm.PlatformThemeFormModel model)
    {
        ThemeWriteModel writeModel = ToWriteModel(model);
        if (ThemeId is null)
        {
            CreateThemeResult result = await ThemeCommand.CreateThemeAsync(writeModel);
            if (!result.Succeeded)
            {
                _ = Snackbar.Add(result.Error ?? Localizer["Error"], Severity.Error);
                return;
            }
        }
        else
        {
            PlatformCommandResult result = await ThemeCommand.UpdateThemeAsync(ThemeId.Value, writeModel);
            if (!result.Succeeded)
            {
                _ = Snackbar.Add(result.Error ?? Localizer["Error"], Severity.Error);
                return;
            }
        }

        _ = Snackbar.Add(Localizer["Theme saved"], Severity.Success);
        NavigationManager.NavigateTo(AppRoutes.PlatformAdmin.Themes);
    }

    private void Cancel() => NavigationManager.NavigateTo(AppRoutes.PlatformAdmin.Themes);

    private static PlatformThemeForm.PlatformThemeFormModel ToFormModel(ThemeEditorModel theme)
        => new()
        {
            Name = theme.Name,
            LightPrimary = theme.LightPalette.Primary,
            LightSecondary = theme.LightPalette.Secondary,
            LightBackground = theme.LightPalette.Background,
            LightSurface = theme.LightPalette.Surface,
            LightTextPrimary = theme.LightPalette.TextPrimary,
            DarkPrimary = theme.DarkPalette.Primary,
            DarkSecondary = theme.DarkPalette.Secondary,
            DarkBackground = theme.DarkPalette.Background,
            DarkSurface = theme.DarkPalette.Surface,
            DarkTextPrimary = theme.DarkPalette.TextPrimary
        };

    private static ThemeWriteModel ToWriteModel(PlatformThemeForm.PlatformThemeFormModel model)
        => new(model.Name,
            new ThemePalette
            {
                Primary = model.LightPrimary,
                PrimaryContrastText = "#FFFFFF",
                Secondary = model.LightSecondary,
                SecondaryContrastText = "#001018",
                Background = model.LightBackground,
                Surface = model.LightSurface,
                AppbarBackground = "#FFFFFF",
                AppbarText = model.LightTextPrimary,
                TextPrimary = model.LightTextPrimary,
                TextSecondary = "#334155",
                Divider = "#E2E8F0"
            },
            new ThemePalette
            {
                Primary = model.DarkPrimary,
                PrimaryContrastText = "#07101F",
                Secondary = model.DarkSecondary,
                SecondaryContrastText = "#041018",
                Background = model.DarkBackground,
                Surface = model.DarkSurface,
                AppbarBackground = "#434343",
                AppbarText = model.DarkTextPrimary,
                TextPrimary = model.DarkTextPrimary,
                TextSecondary = "#94A3B8",
                Divider = "#233044"
            });
}
