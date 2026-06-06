namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemeEditorModel(
    Guid Id,
    string Name,
    ThemePaletteDto LightPalette,
    ThemePaletteDto DarkPalette);
