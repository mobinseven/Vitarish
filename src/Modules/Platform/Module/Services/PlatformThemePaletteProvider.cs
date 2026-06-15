using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Module.Services;

public sealed class PlatformThemePaletteProvider(IPlatformThemeQuery themeQuery) : IPlatformThemePaletteProvider
{
    public async Task<PlatformThemePalette?> GetActivePaletteAsync(CancellationToken cancellationToken = default)
    {
        ThemeEditorModel? theme = await themeQuery.GetActiveThemeAsync(cancellationToken);
        return theme is null
            ? null
            : new PlatformThemePalette(ToPaletteValues(theme.LightPalette), ToPaletteValues(theme.DarkPalette));
    }

    private static ThemePaletteValues ToPaletteValues(ThemePalette palette)
        => new(
            palette.Primary,
            palette.PrimaryContrastText,
            palette.Secondary,
            palette.SecondaryContrastText,
            palette.Tertiary,
            palette.TertiaryContrastText,
            palette.Info,
            palette.InfoContrastText,
            palette.Success,
            palette.SuccessContrastText,
            palette.Warning,
            palette.WarningContrastText,
            palette.Error,
            palette.ErrorContrastText,
            palette.Background,
            palette.Surface,
            palette.AppbarBackground,
            palette.AppbarText,
            palette.TextPrimary,
            palette.TextSecondary,
            palette.Divider);
}
