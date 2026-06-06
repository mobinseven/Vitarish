namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemeWriteModel(
    string Name,
    ThemePaletteDto LightPalette,
    ThemePaletteDto DarkPalette);
