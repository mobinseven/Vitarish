using Microsoft.Extensions.Logging;
using MudBlazor.Utilities;
using Vitarish.Interfaces.Base.Enums;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
public sealed class ThemeService(ILogger<ThemeService> logger) : IThemeService
{
    public ThemeSettings Settings { get; private set; } = ThemeSettings.Default;
    public MudTheme Theme { get; private set; } = BuildTheme(TypographySize.Normal, null);
    private PlatformThemePalette? platformPalette;

    public Variant PrimaryVariant => IsDarkMode ? Variant.Filled : Variant.Filled;
    public Variant SecondaryVariant => IsDarkMode ? Variant.Outlined : Variant.Outlined;

    public bool IsDarkMode { get; private set; }
    public int ThemeVersion { get; private set; }

    public bool ObserveSystemThemeChange => Settings.Theme == ThemePreference.Auto;

    public event Func<ThemeSettings, Task>? SettingsChanged;
    public event Action? VisualStateChanged;

    public Task InitializeAsync(ThemeSettings? imported = null, PlatformThemePalette? platformPalette = null)
    {
        Settings = imported ?? ThemeSettings.Default;
        this.platformPalette = platformPalette;

        Theme = BuildTheme(Settings.Typography, this.platformPalette);
        IsDarkMode = ResolveIsDarkMode(Settings);
        return Task.CompletedTask;
    }
    public Task SetSurfaceAsync(ThemeSurfaceType surface)
    {
        ThemeSettings next = Settings with { Surface = surface };
        return ApplyAsync(next);
    }

    public Task SetThemePreferenceAsync(ThemePreference pref)
    {
        ThemeSettings next = Settings with
        {
            Theme = pref,
            LastKnownIsDarkMode = pref switch
            {
                ThemePreference.Dark => true,
                ThemePreference.Light => false,
                _ => Settings.LastKnownIsDarkMode
            }
        };

        return ApplyAsync(next);
    }

    public Task SetTypographySizeAsync(TypographySize size)
    {
        ThemeSettings next = Settings with { Typography = size };
        return ApplyAsync(next);
    }

    public Task UpdateSystemDarkModeAsync(bool isDarkMode)
    {
        if (Settings.Theme != ThemePreference.Auto)
        {
            return Task.CompletedTask;
        }

        ThemeSettings next = Settings with { LastKnownIsDarkMode = isDarkMode };
        Settings = next;
        ThemeVersion++;
        IsDarkMode = isDarkMode;
        VisualStateChanged?.Invoke();

        return NotifyAsync();
    }

    private Task ApplyAsync(ThemeSettings next)
    {
        Settings = next;
        Theme = BuildTheme(next.Typography, platformPalette);
        IsDarkMode = ResolveIsDarkMode(next);
        ThemeVersion++;
        VisualStateChanged?.Invoke();
        return NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        Func<ThemeSettings, Task>? handlers = SettingsChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (Delegate d in handlers.GetInvocationList())
        {
            try
            {
                await ((Func<ThemeSettings, Task>)d)(Settings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A theme settings subscriber failed while handling a theme update.");
            }
        }
    }

    private static bool ResolveIsDarkMode(ThemeSettings s) => s.Theme switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        ThemePreference.Auto => s.LastKnownIsDarkMode ?? false,
        _ => false
    };

    private static MudTheme BuildTheme(TypographySize size, PlatformThemePalette? platformPalette)
    {
        // Conservative scaling to preserve layout density & avoid component clipping.
        double scale = size switch
        {
            TypographySize.Compact => 0.85,
            TypographySize.Large => 1.25,
            TypographySize.ExtraLarge => 1.5,
            _ => 1.00
        };

        string[] font = new[]
        {
            "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif"
        };

        // Typography in v8 uses BaseTypography and derived types. :contentReference[oaicite:5]{index=5}
        Typography typography = new()
        {
            Default = new DefaultTypography
            {
                FontFamily = font,
                FontSize = Scale("0.875rem", scale),
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "0.00938em",
                TextTransform = "none"
            },
            H1 = new H1Typography { FontFamily = font, FontSize = Scale("2.25rem", scale), FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.015em", TextTransform = "none" },
            H2 = new H2Typography { FontFamily = font, FontSize = Scale("1.875rem", scale), FontWeight = "700", LineHeight = "1.25", LetterSpacing = "-0.01em", TextTransform = "none" },
            H3 = new H3Typography { FontFamily = font, FontSize = Scale("1.5rem", scale), FontWeight = "650", LineHeight = "1.3", LetterSpacing = "-0.005em", TextTransform = "none" },
            H4 = new H4Typography { FontFamily = font, FontSize = Scale("1.25rem", scale), FontWeight = "650", LineHeight = "1.35", LetterSpacing = "0em", TextTransform = "none" },
            H5 = new H5Typography { FontFamily = font, FontSize = Scale("1.125rem", scale), FontWeight = "600", LineHeight = "1.4", LetterSpacing = "0em", TextTransform = "none" },
            H6 = new H6Typography { FontFamily = font, FontSize = Scale("1rem", scale), FontWeight = "600", LineHeight = "1.4", LetterSpacing = "0.005em", TextTransform = "none" },
            Body1 = new Body1Typography { FontFamily = font, FontSize = Scale("0.95rem", scale), FontWeight = "400", LineHeight = "1.55", LetterSpacing = "0.01em", TextTransform = "none" },
            Body2 = new Body2Typography { FontFamily = font, FontSize = Scale("0.875rem", scale), FontWeight = "400", LineHeight = "1.5", LetterSpacing = "0.01em", TextTransform = "none" },
            Subtitle1 = new Subtitle1Typography { FontFamily = font, FontSize = Scale("0.95rem", scale), FontWeight = "600", LineHeight = "1.45", LetterSpacing = "0.01em", TextTransform = "none" },
            Subtitle2 = new Subtitle2Typography { FontFamily = font, FontSize = Scale("0.875rem", scale), FontWeight = "600", LineHeight = "1.45", LetterSpacing = "0.01em", TextTransform = "none" },
            Button = new ButtonTypography { FontFamily = font, FontSize = Scale("0.875rem", scale), FontWeight = "650", LineHeight = "1.2", LetterSpacing = "0.02em", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = font, FontSize = Scale("0.75rem", scale), FontWeight = "400", LineHeight = "1.35", LetterSpacing = "0.02em", TextTransform = "none" },
            Overline = new OverlineTypography { FontFamily = font, FontSize = Scale("0.75rem", scale), FontWeight = "650", LineHeight = "1.2", LetterSpacing = "0.08em", TextTransform = "uppercase" }
        };
        PseudoCss pseudoCss = new()
        { Scope = ":root, :host" };
        return new MudTheme
        {
            PseudoCss = pseudoCss,
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
                AppbarHeight = "64px"
            },
            PaletteLight = new PaletteLight
            {
                Primary = new MudColor(platformPalette?.LightPalette.Primary ?? "#2563EB"),
                PrimaryContrastText = new MudColor(platformPalette?.LightPalette.PrimaryContrastText ?? "#FFFFFF"),
                Secondary = new MudColor(platformPalette?.LightPalette.Secondary ?? "#0EA5E9"),
                SecondaryContrastText = new MudColor(platformPalette?.LightPalette.SecondaryContrastText ?? "#001018"),
                Background = new MudColor(platformPalette?.LightPalette.Background ?? "#f1f1f1"),
                Surface = new MudColor(platformPalette?.LightPalette.Surface ?? "#f1f1f1"),
                AppbarBackground = new MudColor(platformPalette?.LightPalette.AppbarBackground ?? "#FFFFFF"),
                AppbarText = new MudColor(platformPalette?.LightPalette.AppbarText ?? "#0F172A"),
                TextPrimary = new MudColor(platformPalette?.LightPalette.TextPrimary ?? "#0F172A"),
                TextSecondary = new MudColor(platformPalette?.LightPalette.TextSecondary ?? "#334155"),
                Divider = new MudColor(platformPalette?.LightPalette.Divider ?? "#E2E8F0")
            },
            PaletteDark = new PaletteDark
            {
                Primary = new MudColor(platformPalette?.DarkPalette.Primary ?? "#60A5FA"),
                PrimaryContrastText = new MudColor(platformPalette?.DarkPalette.PrimaryContrastText ?? "#07101F"),
                Secondary = new MudColor(platformPalette?.DarkPalette.Secondary ?? "#38BDF8"),
                SecondaryContrastText = new MudColor(platformPalette?.DarkPalette.SecondaryContrastText ?? "#041018"),
                Background = new MudColor(platformPalette?.DarkPalette.Background ?? "#323232"),
                Surface = new MudColor(platformPalette?.DarkPalette.Surface ?? "#323232"),
                AppbarBackground = new MudColor(platformPalette?.DarkPalette.AppbarBackground ?? "#434343"),
                AppbarText = new MudColor(platformPalette?.DarkPalette.AppbarText ?? "#E2E8F0"),
                TextPrimary = new MudColor(platformPalette?.DarkPalette.TextPrimary ?? "#E2E8F0"),
                TextSecondary = new MudColor(platformPalette?.DarkPalette.TextSecondary ?? "#94A3B8"),
                Divider = new MudColor(platformPalette?.DarkPalette.Divider ?? "#233044"),
            },
            Typography = typography
        };
    }

    private static string Scale(string cssSize, double factor)
    {
        if (string.IsNullOrWhiteSpace(cssSize))
        {
            return cssSize;
        }

        static bool TryParseNumber(string s, out double v)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        if (cssSize.EndsWith("rem", StringComparison.OrdinalIgnoreCase))
        {
            string n = cssSize[..^3];
            if (TryParseNumber(n, out double v))
            {
                return (v * factor).ToString("0.###", CultureInfo.InvariantCulture) + "rem";
            }
        }

        if (cssSize.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            string n = cssSize[..^2];
            if (TryParseNumber(n, out double v))
            {
                return (v * factor).ToString("0.###", CultureInfo.InvariantCulture) + "px";
            }
        }

        return cssSize;
    }

    private string ResolveSurfaceStyle() => Settings.Surface switch
    {

        ThemeSurfaceType.Opaque => IsDarkMode ? $@"
  --background: rgba(43, 43, 43, 1);
  --border: none;
  --blur: 0px;
  --brightness: 100%;" : $@"
  --background: rgba(255,255, 255, 1);
  --border: none;
  --blur: 0px;
  --brightness: 100%;",

        ThemeSurfaceType.Transparent => IsDarkMode ? $@"
  --background: rgba(43, 43, 43, .7);
  --border: none;
  --blur: 1px;
  --brightness: 95%;" : $@"
  --background: rgba(255, 255, 255, .7);
  --border: none;
  --blur: 1px;
  --brightness: 105%;",

        _ => IsDarkMode ? $@"
  --background: rgba(43, 43, 43, .85);
  --border: none;
  --blur: 5px;
  --brightness: 110%;" : $@"
  --background: rgba(255, 255, 255, .85);
  --border: none;
  --blur: 5px;
  --brightness: 120%;"
        ,
    };
    public string BuildCustomCss()
    {
        string scope = Theme.PseudoCss?.Scope ?? ":root";

        return $@"
{scope} {{
    {ResolveSurfaceStyle()}
}}";
    }

}
