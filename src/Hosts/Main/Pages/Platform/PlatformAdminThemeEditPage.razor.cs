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
            LightPrimaryContrastText = theme.LightPalette.PrimaryContrastText,
            LightSecondary = theme.LightPalette.Secondary,
            LightSecondaryContrastText = theme.LightPalette.SecondaryContrastText,
            LightTertiary = theme.LightPalette.Tertiary,
            LightTertiaryContrastText = theme.LightPalette.TertiaryContrastText,
            LightInfo = theme.LightPalette.Info,
            LightInfoContrastText = theme.LightPalette.InfoContrastText,
            LightSuccess = theme.LightPalette.Success,
            LightSuccessContrastText = theme.LightPalette.SuccessContrastText,
            LightWarning = theme.LightPalette.Warning,
            LightWarningContrastText = theme.LightPalette.WarningContrastText,
            LightError = theme.LightPalette.Error,
            LightErrorContrastText = theme.LightPalette.ErrorContrastText,
            LightBackground = theme.LightPalette.Background,
            LightSurface = theme.LightPalette.Surface,
            LightTextPrimary = theme.LightPalette.TextPrimary,
            DarkPrimary = theme.DarkPalette.Primary,
            DarkPrimaryContrastText = theme.DarkPalette.PrimaryContrastText,
            DarkSecondary = theme.DarkPalette.Secondary,
            DarkSecondaryContrastText = theme.DarkPalette.SecondaryContrastText,
            DarkTertiary = theme.DarkPalette.Tertiary,
            DarkTertiaryContrastText = theme.DarkPalette.TertiaryContrastText,
            DarkInfo = theme.DarkPalette.Info,
            DarkInfoContrastText = theme.DarkPalette.InfoContrastText,
            DarkSuccess = theme.DarkPalette.Success,
            DarkSuccessContrastText = theme.DarkPalette.SuccessContrastText,
            DarkWarning = theme.DarkPalette.Warning,
            DarkWarningContrastText = theme.DarkPalette.WarningContrastText,
            DarkError = theme.DarkPalette.Error,
            DarkErrorContrastText = theme.DarkPalette.ErrorContrastText,
            DarkBackground = theme.DarkPalette.Background,
            DarkSurface = theme.DarkPalette.Surface,
            DarkTextPrimary = theme.DarkPalette.TextPrimary
        };

    private static ThemeWriteModel ToWriteModel(PlatformThemeForm.PlatformThemeFormModel model)
        => new(model.Name,
            new ThemePalette
            {
                Primary = model.LightPrimary,
                PrimaryContrastText = model.LightPrimaryContrastText,
                Secondary = model.LightSecondary,
                SecondaryContrastText = model.LightSecondaryContrastText,
                Tertiary = model.LightTertiary,
                TertiaryContrastText = model.LightTertiaryContrastText,
                Info = model.LightInfo,
                InfoContrastText = model.LightInfoContrastText,
                Success = model.LightSuccess,
                SuccessContrastText = model.LightSuccessContrastText,
                Warning = model.LightWarning,
                WarningContrastText = model.LightWarningContrastText,
                Error = model.LightError,
                ErrorContrastText = model.LightErrorContrastText,
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
                PrimaryContrastText = model.DarkPrimaryContrastText,
                Secondary = model.DarkSecondary,
                SecondaryContrastText = model.DarkSecondaryContrastText,
                Tertiary = model.DarkTertiary,
                TertiaryContrastText = model.DarkTertiaryContrastText,
                Info = model.DarkInfo,
                InfoContrastText = model.DarkInfoContrastText,
                Success = model.DarkSuccess,
                SuccessContrastText = model.DarkSuccessContrastText,
                Warning = model.DarkWarning,
                WarningContrastText = model.DarkWarningContrastText,
                Error = model.DarkError,
                ErrorContrastText = model.DarkErrorContrastText,
                Background = model.DarkBackground,
                Surface = model.DarkSurface,
                AppbarBackground = "#434343",
                AppbarText = model.DarkTextPrimary,
                TextPrimary = model.DarkTextPrimary,
                TextSecondary = "#94A3B8",
                Divider = "#233044"
            });
}
