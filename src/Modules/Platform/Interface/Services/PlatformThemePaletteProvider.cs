using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Interface.Services;

public sealed class PlatformThemePaletteProvider(IPlatformThemeQuery themeQuery) : IPlatformThemePaletteProvider
{
    public async Task<PlatformThemePalette?> GetActivePaletteAsync(CancellationToken cancellationToken = default)
    {
        ThemeEditorModel? theme = await themeQuery.GetActiveThemeAsync(cancellationToken);
        return theme is null
            ? null
            : new PlatformThemePalette(ToPaletteValues(theme.LightPalette), ToPaletteValues(theme.DarkPalette));
    }

    private static ThemePaletteValues ToPaletteValues(ThemePaletteDto palette)
        => new(
            palette.Primary,
            palette.PrimaryContrastText,
            palette.Secondary,
            palette.SecondaryContrastText,
            palette.Background,
            palette.Surface,
            palette.AppbarBackground,
            palette.AppbarText,
            palette.TextPrimary,
            palette.TextSecondary,
            palette.Divider);
}
