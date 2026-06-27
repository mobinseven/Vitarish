namespace Vitarish.Modules.Aggregator.Interface.Models;

public sealed record ThemePaletteValues(
    string Primary,
    string PrimaryContrastText,
    string Secondary,
    string SecondaryContrastText,
    string Tertiary,
    string TertiaryContrastText,
    string Info,
    string InfoContrastText,
    string Success,
    string SuccessContrastText,
    string Warning,
    string WarningContrastText,
    string Error,
    string ErrorContrastText,
    string Background,
    string Surface,
    string AppbarBackground,
    string AppbarText,
    string TextPrimary,
    string TextSecondary,
    string Divider);

public sealed record PlatformThemePalette(ThemePaletteValues LightPalette, ThemePaletteValues DarkPalette);
