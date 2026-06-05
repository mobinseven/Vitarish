namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemeListItem(
    Guid Id,
    string Name,
    ThemePaletteDto LightPalette,
    ThemePaletteDto DarkPalette,
    bool IsActive);
