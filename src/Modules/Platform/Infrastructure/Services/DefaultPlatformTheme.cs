using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Infrastructure.Services;

[Obsolete("Platform theme bootstrap is obsolete; retain existing data for compatibility only.")]
public static class DefaultPlatformTheme
{
    public static readonly Guid Id = Guid.Parse("3af03f4e-e53a-4f63-97cc-820c5ea52d85");
    public const string Name = "Default";

    public static Theme Create() => new()
    {
        Id = Id,
        Name = Name,
        LightPalette = new ThemePalette(),
        DarkPalette = CreateDarkPalette()
    };

    public static ThemePalette CreateDarkPalette() => new()
    {
        Primary = ThemePalette.FallbackDarkPrimary,
        PrimaryContrastText = ThemePalette.FallbackDarkPrimaryContrastText,
        Secondary = ThemePalette.FallbackDarkSecondary,
        SecondaryContrastText = ThemePalette.FallbackDarkSecondaryContrastText,
        Tertiary = ThemePalette.FallbackDarkTertiary,
        TertiaryContrastText = ThemePalette.FallbackDarkTertiaryContrastText,
        Info = ThemePalette.FallbackDarkInfo,
        InfoContrastText = ThemePalette.FallbackDarkInfoContrastText,
        Success = ThemePalette.FallbackDarkSuccess,
        SuccessContrastText = ThemePalette.FallbackDarkSuccessContrastText,
        Warning = ThemePalette.FallbackDarkWarning,
        WarningContrastText = ThemePalette.FallbackDarkWarningContrastText,
        Error = ThemePalette.FallbackDarkError,
        ErrorContrastText = ThemePalette.FallbackDarkErrorContrastText,
        Background = ThemePalette.FallbackDarkBackground,
        Surface = ThemePalette.FallbackDarkSurface,
        AppbarBackground = ThemePalette.FallbackDarkAppbarBackground,
        AppbarText = ThemePalette.FallbackDarkAppbarText,
        TextPrimary = ThemePalette.FallbackDarkTextPrimary,
        TextSecondary = ThemePalette.FallbackDarkTextSecondary,
        Divider = ThemePalette.FallbackDarkDivider
    };
}
