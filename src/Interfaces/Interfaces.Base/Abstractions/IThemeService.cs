using Vitarish.Interfaces.Base.Enums;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;


public interface IThemeService
{
    ThemeSettings Settings { get; }
    MudTheme Theme { get; }
    bool IsDarkMode { get; }
    bool ObserveSystemThemeChange { get; }
    int ThemeVersion { get; }
    Variant PrimaryVariant { get; }
    Variant SecondaryVariant { get; }

    /// <summary>Raised for any change that should be persisted/exported.</summary>
    event Func<ThemeSettings, Task>? SettingsChanged;

    /// <summary>Raised whenever UI should re-render (theme/IsDarkMode changed).</summary>
    event Action? VisualStateChanged;

    string BuildCustomCss();
    Task InitializeAsync(ThemeSettings? imported = null);
    Task SetSurfaceAsync(ThemeSurfaceType surface);
    Task SetThemePreferenceAsync(ThemePreference pref);
    Task SetTypographySizeAsync(TypographySize size);

    /// <summary>Called by the layout connector when the system theme toggles (Auto).</summary>
    Task UpdateSystemDarkModeAsync(bool isDarkMode);
}
