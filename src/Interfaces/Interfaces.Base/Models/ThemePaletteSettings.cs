namespace Vitarish.Interfaces.Base.Models;

public sealed record ThemePaletteValues(
    string Primary,
    string PrimaryContrastText,
    string Secondary,
    string SecondaryContrastText,
    string Background,
    string Surface,
    string AppbarBackground,
    string AppbarText,
    string TextPrimary,
    string TextSecondary,
    string Divider);

public sealed record PlatformThemePalette(ThemePaletteValues LightPalette, ThemePaletteValues DarkPalette);
