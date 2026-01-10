using MudBlazor.Utilities;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Enums;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
public sealed class ThemeService : IThemeService
{
    public ThemeSettings Settings { get; private set; } = ThemeSettings.Default;
    public MudTheme Theme { get; private set; } = BuildTheme(TypographySize.Normal);

    public bool IsDarkMode { get; private set; }
    public int ThemeVersion { get; private set; }

    // MudThemeProvider.ObserveSystemThemeChange defaults to true; we explicitly enable only for Auto. :contentReference[oaicite:4]{index=4}
    public bool ObserveSystemThemeChange => Settings.Theme == ThemePreference.Auto;

    public event Func<ThemeSettings, Task>? SettingsChanged;
    public event Action? VisualStateChanged;

    public async Task InitializeAsync(ThemeSettings? imported = null)
    {
        Settings = imported ?? ThemeSettings.Default;

        Theme = BuildTheme(Settings.Typography);
        IsDarkMode = ResolveIsDarkMode(Settings);
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
        // Only persist system changes when following system (Auto)
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
        Theme = BuildTheme(next.Typography);
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
            { await ((Func<ThemeSettings, Task>)d)(Settings); }
            catch { /* swallow/log as you prefer */ }
        }
    }

    private static bool ResolveIsDarkMode(ThemeSettings s) => s.Theme switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        ThemePreference.Auto => s.LastKnownIsDarkMode ?? false,
        _ => false
    };

    private static MudTheme BuildTheme(TypographySize size)
    {
        // Conservative scaling to preserve layout density & avoid component clipping.
        var scale = size switch
        {
            TypographySize.Compact => 0.85,
            TypographySize.Large => 1.15,
            _ => 1.00
        };

        var font = new[]
        {
            "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif"
        };

        // Typography in v8 uses BaseTypography and derived types. :contentReference[oaicite:5]{index=5}
        var typography = new Typography
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
                Primary = new MudColor("#2563EB"),
                PrimaryContrastText = new MudColor("#FFFFFF"),
                Secondary = new MudColor("#0EA5E9"),
                SecondaryContrastText = new MudColor("#001018"),
                Background = new MudColor("#F8FAFC"),
                Surface = new MudColor("#FFFFFF"),
                AppbarBackground = new MudColor("#FFFFFF"),
                AppbarText = new MudColor("#0F172A"),
                TextPrimary = new MudColor("#0F172A"),
                TextSecondary = new MudColor("#334155"),
                Divider = new MudColor("#E2E8F0")
            },
            PaletteDark = new PaletteDark
            {
                Primary = new MudColor("#60A5FA"),
                PrimaryContrastText = new MudColor("#07101F"),
                Secondary = new MudColor("#38BDF8"),
                SecondaryContrastText = new MudColor("#041018"),
                Background = new MudColor("#333333"),
                Surface = new MudColor("#0F172A"),
                AppbarBackground = new MudColor("#0F172A"),
                AppbarText = new MudColor("#E2E8F0"),
                TextPrimary = new MudColor("#E2E8F0"),
                TextSecondary = new MudColor("#94A3B8"),
                Divider = new MudColor("#233044")
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
            var n = cssSize[..^3];
            if (TryParseNumber(n, out var v))
            {
                return (v * factor).ToString("0.###", CultureInfo.InvariantCulture) + "rem";
            }
        }

        if (cssSize.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            var n = cssSize[..^2];
            if (TryParseNumber(n, out var v))
            {
                return (v * factor).ToString("0.###", CultureInfo.InvariantCulture) + "px";
            }
        }

        return cssSize;
    }

    private string ResolveSurfaceStyle() => Settings.Surface switch
    {

        ThemeSurfaceType.Opaque => IsDarkMode ? $@"
  --background: rgba(18, 18, 18, 1);
  --border: 1px solid rgba(93, 93, 93, .5);
  --blur: 0px;
  --brightness: 100%;" : $@"
  --background: rgba(255,255, 255, 1);
  --border: 1px solid rgba(18, 18, 18, .55);
  --blur: 0px;
  --brightness: 100%;",

        ThemeSurfaceType.Transparent => IsDarkMode ? $@"
  --background: rgba(18, 18, 18, .2);
  --border: 1px solid rgba(93, 93, 93, .8);
  --blur: 1px;
  --brightness: 95%;" : $@"
  --background: rgba(255, 255, 255, .2);
  --border: 1px solid rgba(18, 18, 18, .8);
  --blur: 1px;
  --brightness: 105%;",

        _ => IsDarkMode ? $@"
  --background: rgba(18, 18, 18, .5);
  --border: 1px solid rgba(93, 93, 93, .5);
  --blur: 5px;
  --brightness: 80%;" : $@"
  --background: rgba(255, 255, 255, .5);
  --border: 1px solid rgba(18, 18, 18, .5);
  --blur: 5px;
  --brightness: 100%;"
        ,
    };
    public string BuildCustomCss()
    {
        var scope = Theme.PseudoCss?.Scope ?? ":root";

        return $@"
{scope} {{
    {ResolveSurfaceStyle()}
}}";
    }

}
