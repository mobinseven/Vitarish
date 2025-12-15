namespace Vitarish.Clients.Interface.Services;

[RegisterScoped]
internal sealed class ThemeService : IThemeService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly ILayoutService layoutService;

    public ThemeService(ILayoutService layoutService)
    {
        this.layoutService = layoutService;
        Theme = new MudTheme
        {
            PaletteDark = new PaletteDark
            {
                Primary = Colors.Blue.Darken3,
                Secondary = "#80cbc4",
                Tertiary = "#4dd0e1",
                Info = "#4fc3f7",
                Success = Colors.Teal.Accent4,
                Warning = "#ffb74d",
                Error = "#cf6679",
                Dark = "#0f1214",
                Background = "#121212",
                Surface = "#1e1e1e",
                TextPrimary = "#f5f5f5",
            },
            PaletteLight = new PaletteLight
            {
                Primary = Colors.Blue.Darken3,
                Secondary = "#00897b",
                Tertiary = "#00acc1",
                Info = "#0288d1",
                Success = Colors.Teal.Accent4,
                Warning = "#fb8c00",
                Error = "#800413",
                Dark = "#263238",
                Surface = "#ffffff",
                TextPrimary = Colors.Gray.Darken4,
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "20px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Ahang", "SegoeUI", "sans-serif"],
                    FontSize = "1rem",
                    FontWeight = "500"
                }
            }
        };

        Light = new ThemeSurface(
            blur: "20px",
            brightness: "110%",
            background: "rgba(255, 255, 255, .45)",
            border: "1px solid rgba(255, 255, 255, .55)",
            shimmer: new ShimmerStyle(
                TextColor: "white",
                Background: "#222222 -webkit-gradient(linear, 100% 0, 0 0, from(#222222), color-stop(0.5, #ffffff), to(#222222))"));

        Dark = new ThemeSurface(
            blur: "20px",
            brightness: "100%",
            background: "rgba(18, 18, 18, .75)",
            border: "1px solid rgba(255, 255, 255, .25)",
            shimmer: new ShimmerStyle(
                TextColor: "#f5f5f5",
                Background: "#2a2a2a -webkit-gradient(linear, 100% 0, 0 0, from(#2a2a2a), color-stop(0.5, #f5f5f5), to(#2a2a2a))"));

        Typography = new TypographyVariables(
            fontSize: Theme.Typography.Default.FontSize ?? "1rem",
            fontWeight: Theme.Typography.Default.FontWeight ?? "500",
            lineHeight: "1.5");
    }

    public MudTheme Theme { get; }

    public ThemeSurface Light { get; }

    public ThemeSurface Dark { get; }

    public TypographyVariables Typography { get; }

    public bool DarkModeEnabled { get; private set; }

    public bool BlazorReadyEnabled { get; private set; } = true;

    public RenderFragment? NavMenuButtons { get; private set; }

    public ThemeSurface GetSurface(bool darkMode) => darkMode ? Dark : Light;

    public void SetDarkMode(bool enabled)
    {
        if (DarkModeEnabled == enabled)
        {
            return;
        }

        DarkModeEnabled = enabled;
        layoutService.NotifyThemeStateChanged(this);
    }

    public void SetBlazorReadyEnabled(bool enabled)
    {
        if (BlazorReadyEnabled == enabled)
        {
            return;
        }

        BlazorReadyEnabled = enabled;
        layoutService.NotifyThemeStateChanged(this);
    }

    public void SetNavMenuButtons(RenderFragment? buttons)
    {
        if (ReferenceEquals(NavMenuButtons, buttons))
        {
            return;
        }

        NavMenuButtons = buttons;
        layoutService.NotifyThemeStateChanged(this);
    }

    public void UpdateLightPaletteColors(
        string? primary = null,
        string? secondary = null,
        string? tertiary = null,
        string? info = null,
        string? success = null,
        string? warning = null,
        string? error = null,
        string? dark = null,
        string? surface = null,
        string? textPrimary = null)
    {
        if (UpdatePalette(
                Theme.PaletteLight,
                primary,
                secondary,
                tertiary,
                info,
                success,
                warning,
                error,
                dark,
                surface,
                textPrimary))
        {
            layoutService.NotifyThemeStateChanged(this);
        }
    }

    public void UpdateDarkPaletteColors(
        string? primary = null,
        string? secondary = null,
        string? tertiary = null,
        string? info = null,
        string? success = null,
        string? warning = null,
        string? error = null,
        string? dark = null,
        string? surface = null,
        string? textPrimary = null)
    {
        if (UpdatePalette(
                Theme.PaletteDark,
                primary,
                secondary,
                tertiary,
                info,
                success,
                warning,
                error,
                dark,
                surface,
                textPrimary))
        {
            layoutService.NotifyThemeStateChanged(this);
        }
    }

    /// <summary>
    /// Updates typography settings (font size and weight) for the theme.
    /// This affects the global font size of MudBlazor components via CSS custom properties.
    /// To scale the entire UI, adjust the fontSize parameter (e.g., "0.875rem" for compact, "1.125rem" for large).
    /// </summary>
    /// <param name="fontSize">Font size for default typography (e.g., "1rem", "0.875rem")</param>
    /// <param name="fontWeight">Font weight for default typography (e.g., "400", "500", "600")</param>
    public void UpdateTypography(string? fontSize = null, string? fontWeight = null)
    {
        bool updated = false;

        if (!string.IsNullOrWhiteSpace(fontSize))
        {
            Theme.Typography.Default.FontSize = fontSize;
            Typography.Update(fontSize: fontSize);
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(fontWeight))
        {
            Theme.Typography.Default.FontWeight = fontWeight;
            Typography.Update(fontWeight: fontWeight);
            updated = true;
        }

        if (updated)
        {
            layoutService.NotifyThemeStateChanged(this);
        }
    }

    public void UpdateLightSurface(string? blur = null, string? brightness = null, string? background = null, string? border = null)
    {
        if (UpdateSurface(Light, blur, brightness, background, border))
        {
            layoutService.NotifyThemeStateChanged(this);
        }
    }

    public void UpdateDarkSurface(string? blur = null, string? brightness = null, string? background = null, string? border = null)
    {
        if (UpdateSurface(Dark, blur, brightness, background, border))
        {
            layoutService.NotifyThemeStateChanged(this);
        }
    }

    public string GetThemeSettingsJson()
    {
        ThemeSettingsDto settings = new()
        {
            DarkModeEnabled = DarkModeEnabled,
            BlazorReadyEnabled = BlazorReadyEnabled,
            PaletteLight = CreatePaletteSettings(Theme.PaletteLight),
            PaletteDark = CreatePaletteSettings(Theme.PaletteDark),
            Typography = new TypographySettingsDto
            {
                FontSize = Theme.Typography.Default.FontSize ?? string.Empty,
                FontWeight = Theme.Typography.Default.FontWeight ?? string.Empty,
            },
            LightSurface = CreateSurfaceSettings(Light),
            DarkSurface = CreateSurfaceSettings(Dark),
        };

        return JsonSerializer.Serialize(settings, SerializerOptions);
    }

    public void LoadThemeSettingsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON cannot be null or whitespace.", nameof(json));
        }

        ThemeSettingsDto? settings;

        try
        {
            settings = JsonSerializer.Deserialize<ThemeSettingsDto>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON did not contain valid theme settings.", nameof(json), exception);
        }

        if (settings is null)
        {
            throw new ArgumentException("JSON did not contain valid theme settings.", nameof(json));
        }

        if (settings.PaletteLight is { } paletteLight)
        {
            ApplyPalette(Theme.PaletteLight, paletteLight);
        }

        if (settings.PaletteDark is { } paletteDark)
        {
            ApplyPalette(Theme.PaletteDark, paletteDark);
        }

        if (settings.Typography is { } typography)
        {
            Theme.Typography.Default.FontSize = SelectValue(
                typography.FontSize,
                Theme.Typography.Default.FontSize ?? string.Empty);
            Theme.Typography.Default.FontWeight = SelectValue(
                typography.FontWeight,
                Theme.Typography.Default.FontWeight ?? string.Empty);

            // Update typography CSS variables
            Typography.Update(
                fontSize: Theme.Typography.Default.FontSize,
                fontWeight: Theme.Typography.Default.FontWeight);
        }

        ApplySurface(Light, settings.LightSurface);
        ApplySurface(Dark, settings.DarkSurface);

        DarkModeEnabled = settings.DarkModeEnabled ?? DarkModeEnabled;
        BlazorReadyEnabled = settings.BlazorReadyEnabled ?? BlazorReadyEnabled;

        layoutService.NotifyThemeStateChanged(this);
    }

    private static bool UpdateSurface(
        ThemeSurface surface,
        string? blur,
        string? brightness,
        string? background,
        string? border)
    {
        bool hasUpdates = false;

        if (!string.IsNullOrWhiteSpace(blur) && !string.Equals(surface.Blur, blur, StringComparison.Ordinal))
        {
            hasUpdates = true;
        }
        else
        {
            blur = null;
        }

        if (!string.IsNullOrWhiteSpace(brightness) && !string.Equals(surface.Brightness, brightness, StringComparison.Ordinal))
        {
            hasUpdates = true;
        }
        else
        {
            brightness = null;
        }

        if (!string.IsNullOrWhiteSpace(background) && !string.Equals(surface.Background, background, StringComparison.Ordinal))
        {
            hasUpdates = true;
        }
        else
        {
            background = null;
        }

        if (!string.IsNullOrWhiteSpace(border) && !string.Equals(surface.Border, border, StringComparison.Ordinal))
        {
            hasUpdates = true;
        }
        else
        {
            border = null;
        }

        if (!hasUpdates)
        {
            return false;
        }

        surface.Update(blur, brightness, background, border);
        return true;
    }

    private static bool UpdatePalette(
        Palette palette,
        string? primary,
        string? secondary,
        string? tertiary,
        string? info,
        string? success,
        string? warning,
        string? error,
        string? dark,
        string? surface,
        string? textPrimary)
    {
        bool updated = false;

        if (!string.IsNullOrWhiteSpace(primary))
        {
            palette.Primary = primary;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(secondary))
        {
            palette.Secondary = secondary;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(tertiary))
        {
            palette.Tertiary = tertiary;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(info))
        {
            palette.Info = info;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(success))
        {
            palette.Success = success;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(warning))
        {
            palette.Warning = warning;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            palette.Error = error;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(dark))
        {
            palette.Dark = dark;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(surface))
        {
            palette.Surface = surface;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(textPrimary))
        {
            palette.TextPrimary = textPrimary;
            updated = true;
        }

        return updated;
    }

    private static PaletteSettingsDto CreatePaletteSettings(Palette palette) => new()
    {
        Primary = palette.Primary.Value,
        Secondary = palette.Secondary.Value,
        Tertiary = palette.Tertiary.Value,
        Info = palette.Info.Value,
        Success = palette.Success.Value,
        Warning = palette.Warning.Value,
        Error = palette.Error.Value,
        Dark = palette.Dark.Value,
        Surface = palette.Surface.Value,
        TextPrimary = palette.TextPrimary.Value,
    };

    private static SurfaceSettingsDto CreateSurfaceSettings(ThemeSurface surface) => new()
    {
        Blur = surface.Blur,
        Brightness = surface.Brightness,
        Background = surface.Background,
        Border = surface.Border,
    };

    private static void ApplyPalette(Palette palette, PaletteSettingsDto settings)
    {
        palette.Primary = SelectValue(settings.Primary, palette.Primary.Value);
        palette.Secondary = SelectValue(settings.Secondary, palette.Secondary.Value);
        palette.Tertiary = SelectValue(settings.Tertiary, palette.Tertiary.Value);
        palette.Info = SelectValue(settings.Info, palette.Info.Value);
        palette.Success = SelectValue(settings.Success, palette.Success.Value);
        palette.Warning = SelectValue(settings.Warning, palette.Warning.Value);
        palette.Error = SelectValue(settings.Error, palette.Error.Value);
        palette.Dark = SelectValue(settings.Dark, palette.Dark.Value);
        palette.Surface = SelectValue(settings.Surface, palette.Surface.Value);
        palette.TextPrimary = SelectValue(settings.TextPrimary, palette.TextPrimary.Value);
    }

    private static void ApplySurface(ThemeSurface surface, SurfaceSettingsDto? settings)
    {
        if (settings is null)
        {
            return;
        }

        surface.Update(settings.Blur, settings.Brightness, settings.Background, settings.Border);
    }

    private static string SelectValue(string? candidate, string fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

    private sealed class ThemeSettingsDto
    {
        public bool? DarkModeEnabled { get; init; }

        public bool? BlazorReadyEnabled { get; init; }

        public PaletteSettingsDto? PaletteLight { get; init; }

        public PaletteSettingsDto? PaletteDark { get; init; }

        public TypographySettingsDto? Typography { get; init; }

        public SurfaceSettingsDto? LightSurface { get; init; }

        public SurfaceSettingsDto? DarkSurface { get; init; }
    }

    private sealed class PaletteSettingsDto
    {
        public string? Primary { get; init; }

        public string? Secondary { get; init; }

        public string? Tertiary { get; init; }

        public string? Info { get; init; }

        public string? Success { get; init; }

        public string? Warning { get; init; }

        public string? Error { get; init; }

        public string? Dark { get; init; }

        public string? Surface { get; init; }

        public string? TextPrimary { get; init; }
    }

    private sealed class TypographySettingsDto
    {
        public string? FontSize { get; init; }

        public string? FontWeight { get; init; }
    }

    private sealed class SurfaceSettingsDto
    {
        public string? Blur { get; init; }

        public string? Brightness { get; init; }

        public string? Background { get; init; }

        public string? Border { get; init; }
    }
}

public interface IThemeService
{
    MudTheme Theme { get; }
    ThemeSurface Light { get; }
    ThemeSurface Dark { get; }
    TypographyVariables Typography { get; }
    bool DarkModeEnabled { get; }
    bool BlazorReadyEnabled { get; }
    RenderFragment? NavMenuButtons { get; }
    ThemeSurface GetSurface(bool darkMode);
    void SetDarkMode(bool enabled);
    void SetBlazorReadyEnabled(bool enabled);
    void SetNavMenuButtons(RenderFragment? buttons);
    void UpdateLightPaletteColors(
        string? primary = null,
        string? secondary = null,
        string? tertiary = null,
        string? info = null,
        string? success = null,
        string? warning = null,
        string? error = null,
        string? dark = null,
        string? surface = null,
        string? textPrimary = null);
    void UpdateDarkPaletteColors(
        string? primary = null,
        string? secondary = null,
        string? tertiary = null,
        string? info = null,
        string? success = null,
        string? warning = null,
        string? error = null,
        string? dark = null,
        string? surface = null,
        string? textPrimary = null);
    void UpdateTypography(string? fontSize = null, string? fontWeight = null);
    void UpdateLightSurface(string? blur = null, string? brightness = null, string? background = null, string? border = null);
    void UpdateDarkSurface(string? blur = null, string? brightness = null, string? background = null, string? border = null);
    string GetThemeSettingsJson();
    void LoadThemeSettingsJson(string json);
}

public sealed class ThemeSurface
{
    private readonly Dictionary<string, string> rootVariables;

    public ThemeSurface(string blur, string brightness, string background, string border, ShimmerStyle shimmer)
    {
        Blur = blur;
        Brightness = brightness;
        Background = background;
        Border = border;
        Shimmer = shimmer;

        rootVariables = new Dictionary<string, string>
        {
            ["--blur"] = blur,
            ["--brightness"] = brightness,
            ["--background"] = background,
            ["--border"] = border,
        };
    }

    public string Blur { get; private set; }

    public string Brightness { get; private set; }

    public string Background { get; private set; }

    public string Border { get; private set; }

    public ShimmerStyle Shimmer { get; }

    public IReadOnlyDictionary<string, string> RootVariables => rootVariables;

    public void Update(string? blur = null, string? brightness = null, string? background = null, string? border = null)
    {
        if (!string.IsNullOrWhiteSpace(blur))
        {
            Blur = blur;
            rootVariables["--blur"] = blur;
        }

        if (!string.IsNullOrWhiteSpace(brightness))
        {
            Brightness = brightness;
            rootVariables["--brightness"] = brightness;
        }

        if (!string.IsNullOrWhiteSpace(background))
        {
            Background = background;
            rootVariables["--background"] = background;
        }

        if (!string.IsNullOrWhiteSpace(border))
        {
            Border = border;
            rootVariables["--border"] = border;
        }
    }
}

public sealed record ShimmerStyle(string TextColor, string Background);

/// <summary>
/// Manages typography-related CSS custom properties for consistent font sizing across MudBlazor components.
/// 
/// TYPOGRAPHY THEME CONFIGURATION GUIDE:
/// ======================================
/// To change the global font size scale for all MudBlazor components, modify these theme properties:
/// 
/// 1. ThemeService.UpdateTypography(fontSize: "...", fontWeight: "...")
///    - fontSize: Controls the base font size (default: "1rem")
///      * Compact UI: "0.875rem" (14px)
///      * Default UI: "1rem" (16px)
///      * Comfortable UI: "1.125rem" (18px)
///      * Large UI: "1.25rem" (20px)
///    - fontWeight: Controls the base font weight (default: "500")
///      * Light: "300"
///      * Normal: "400"
///      * Medium: "500"
///      * Semi-bold: "600"
/// 
/// 2. The following CSS custom properties are automatically updated:
///    - --mud-typography-default-size: Base font size for most components
///    - --mud-typography-default-weight: Base font weight
///    - --mud-typography-default-lineheight: Base line height (fixed at 1.5)
/// 
/// 3. These variables are used by MudBlazor components including:
///    - MudText, MudButton, MudTextField, MudSelect, MudAutocomplete
///    - MudTable, MudDataGrid, MudChip, MudBadge
///    - MudMenuItem, MudNavLink, MudDrawer
///    - And many other components
/// 
/// Example usage:
///   // Compact theme for dense UIs
///   themeService.UpdateTypography(fontSize: "0.875rem", fontWeight: "400");
///   
///   // Large theme for accessibility
///   themeService.UpdateTypography(fontSize: "1.25rem", fontWeight: "500");
/// </summary>
public sealed class TypographyVariables
{
    // Dictionary used instead of FrozenDictionary to allow runtime updates via Update() method
    private readonly Dictionary<string, string> rootVariables;

    public TypographyVariables(string fontSize, string fontWeight, string lineHeight)
    {
        FontSize = ValidateCssValue(fontSize, nameof(fontSize));
        FontWeight = ValidateCssValue(fontWeight, nameof(fontWeight));
        LineHeight = ValidateCssValue(lineHeight, nameof(lineHeight));

        rootVariables = new Dictionary<string, string>
        {
            ["--mud-typography-default-size"] = FontSize,
            ["--mud-typography-default-weight"] = FontWeight,
            ["--mud-typography-default-lineheight"] = LineHeight
        };
    }

    public string FontSize { get; private set; }

    public string FontWeight { get; private set; }

    public string LineHeight { get; private set; }

    /// <summary>
    /// Gets the CSS custom properties as a read-only dictionary.
    /// The returned dictionary is read-only to callers, but its contents may be updated internally
    /// by this class via the <see cref="Update"/> method. Note: Dictionary is used (not FrozenDictionary)
    /// because this class needs to modify the dictionary contents after construction. The cast to 
    /// IReadOnlyDictionary only prevents modification by callers, not by the class itself.
    /// </summary>
    public IReadOnlyDictionary<string, string> RootVariables => rootVariables;

    public void Update(string? fontSize = null, string? fontWeight = null, string? lineHeight = null)
    {
        if (!string.IsNullOrWhiteSpace(fontSize))
        {
            string validatedFontSize = ValidateCssValue(fontSize, nameof(fontSize));
            FontSize = validatedFontSize;
            rootVariables["--mud-typography-default-size"] = validatedFontSize;
        }

        if (!string.IsNullOrWhiteSpace(fontWeight))
        {
            string validatedFontWeight = ValidateCssValue(fontWeight, nameof(fontWeight));
            FontWeight = validatedFontWeight;
            rootVariables["--mud-typography-default-weight"] = validatedFontWeight;
        }

        if (!string.IsNullOrWhiteSpace(lineHeight))
        {
            string validatedLineHeight = ValidateCssValue(lineHeight, nameof(lineHeight));
            LineHeight = validatedLineHeight;
            rootVariables["--mud-typography-default-lineheight"] = validatedLineHeight;
        }
    }

    private static readonly System.Text.RegularExpressions.Regex CssValuePattern =
        new(@"^[\w\s.,%()+\-]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly char[] DangerousChars = { ';', '{', '}' };

    /// <summary>
    /// Validates CSS value to prevent CSS injection attacks.
    /// Ensures the value contains only safe characters for CSS property values.
    /// </summary>
    private static string ValidateCssValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("CSS value cannot be null or whitespace.", parameterName);
        }

        // Allow alphanumeric, periods, hyphens, parentheses, commas, spaces, %, and + for valid CSS values
        // Examples: "1rem", "1.5", "500", "calc(1rem + 2px)", "100%", "sans-serif"
        // Note: Spaces are allowed for calc() expressions and font-family names
        // Security: Semicolons and braces are blocked separately to prevent CSS injection
        if (!CssValuePattern.IsMatch(value))
        {
            throw new ArgumentException(
                $"CSS value '{value}' contains invalid characters. Only alphanumeric characters, spaces, periods, hyphens, parentheses, commas, and % are allowed.",
                parameterName);
        }

        // Prevent CSS injection by checking for suspicious patterns
        return value.IndexOfAny(DangerousChars) >= 0
            ? throw new ArgumentException(
                $"CSS value '{value}' contains potentially malicious characters (semicolons or braces).",
                parameterName)
            : value.Trim();
    }
}
