namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemePaletteDto(
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
