using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class AccessibilitySettingsForm
{
    private const string PreferenceKey = "Accessibility";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly Dictionary<string, string> preferences = new(StringComparer.OrdinalIgnoreCase);
    private AccessibilitySettings settings = new();
    private AccessibilitySettings initialSettings = new();
    private bool loading = true;
    private bool saving;
    private string defaultLightText = "#111111";
    private string defaultDarkText = "#f5f5f5";
    private string lightBaseBackground = "rgba(255, 255, 255, 0.45)";
    private string darkBaseBackground = "rgba(18, 18, 18, 0.75)";

    private string PreviewStyle =>
        $"background:{BuildBackground(lightBaseBackground)};border:{BuildBorder(isDark: false)};" +
        $"filter:brightness({settings.Brightness.ToString("0", CultureInfo.InvariantCulture)}%);" +
        $"backdrop-filter:blur({settings.SurfaceBlur.ToString("0.#", CultureInfo.InvariantCulture)}px);" +
        $"line-height:{settings.LineHeight.ToString("0.##", CultureInfo.InvariantCulture)};";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (User is null)
        {
            loading = false;
            return;
        }

        CacheThemeDefaults();
        Dictionary<string, string>? storedPreferences = await UserService.GetProfileAsync(User.Id);
        if (storedPreferences is not null)
        {
            foreach ((string key, string value) in storedPreferences)
            {
                preferences[key] = value;
            }
        }

        settings = CreateDefaultsFromTheme();
        if (preferences.TryGetValue(PreferenceKey, out string? storedJson)
            && TryDeserialize(storedJson, out AccessibilitySettings? storedSettings)
            && storedSettings is not null)
        {
            settings = storedSettings;
        }

        initialSettings = settings.Clone();
        ApplySettings();
        loading = false;
    }

    private void CacheThemeDefaults()
    {
        defaultLightText = ThemeService.Theme.PaletteLight?.TextPrimary?.Value ?? defaultLightText;
        defaultDarkText = ThemeService.Theme.PaletteDark?.TextPrimary?.Value ?? defaultDarkText;
        lightBaseBackground = ThemeService.Light.Background ?? lightBaseBackground;
        darkBaseBackground = ThemeService.Dark.Background ?? darkBaseBackground;
    }

    private AccessibilitySettings CreateDefaultsFromTheme()
    {
        string? defaultFontSize = ThemeService.Theme.Typography.Default.FontSize;
        string? defaultFontWeight = ThemeService.Theme.Typography.Default.FontWeight;
        double parsedOpacity = ParseOpacity(lightBaseBackground, 0.65);

        return new AccessibilitySettings
        {
            FontScale = ParseCssNumber(defaultFontSize, "rem", 1d),
            FontWeight = (int)Math.Round(ParseCssNumber(defaultFontWeight, string.Empty, 500d)),
            LineHeight = ParseCssNumber(ThemeService.Typography.LineHeight, string.Empty, 1.5d),
            SurfaceOpacity = parsedOpacity,
            SurfaceBlur = ParseCssNumber(ThemeService.Light.Blur, "px", 20d),
            Brightness = ParseCssNumber(ThemeService.Light.Brightness, "%", 100d),
            HighContrast = false
        };
    }

    private async Task ApplyAfterChangeAsync()
    {
        ApplySettings();
        await InvokeAsync(StateHasChanged);
    }

    private void ApplySettings()
    {
        if (User is null)
        {
            return;
        }

        ThemeService.UpdateTypography(
            $"{settings.FontScale.ToString("0.##", CultureInfo.InvariantCulture)}rem",
            settings.FontWeight.ToString(CultureInfo.InvariantCulture));

        ThemeService.Typography.Update(
            lineHeight: settings.LineHeight.ToString("0.##", CultureInfo.InvariantCulture));

        string blurValue = $"{settings.SurfaceBlur.ToString("0.#", CultureInfo.InvariantCulture)}px";
        string brightnessValue = $"{settings.Brightness.ToString("0.#", CultureInfo.InvariantCulture)}%";

        ThemeService.UpdateLightSurface(
            blur: blurValue,
            brightness: brightnessValue,
            background: BuildBackground(lightBaseBackground),
            border: BuildBorder(isDark: false));

        ThemeService.UpdateDarkSurface(
            blur: blurValue,
            brightness: brightnessValue,
            background: BuildBackground(darkBaseBackground),
            border: BuildBorder(isDark: true));

        ThemeService.UpdateLightPaletteColors(
            textPrimary: settings.HighContrast ? "#000000" : defaultLightText);

        ThemeService.UpdateDarkPaletteColors(
            textPrimary: settings.HighContrast ? "#ffffff" : defaultDarkText);
    }

    private string BuildBackground(string source)
    {
        (int r, int g, int b) = ParseRgb(source) ?? (255, 255, 255);
        return $"rgba({r}, {g}, {b}, {settings.SurfaceOpacity.ToString("0.##", CultureInfo.InvariantCulture)})";
    }

    private string BuildBorder(bool isDark)
    {
        double alpha = Math.Min(0.15 + (settings.SurfaceOpacity / (isDark ? 2.5 : 3.5)), isDark ? 0.65 : 0.45);
        string channel = isDark ? "255, 255, 255" : "0, 0, 0";
        return $"1px solid rgba({channel}, {alpha.ToString("0.##", CultureInfo.InvariantCulture)})";
    }

    private static double ParseCssNumber(string? value, string suffix, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string trimmed = value.Trim();
        if (!string.IsNullOrEmpty(suffix) && trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^suffix.Length];
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : fallback;
    }

    private static (int r, int g, int b)? ParseRgb(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Match match = Regex.Match(
            value,
            @"rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})",
            RegexOptions.IgnoreCase);

        return match.Success
            ? (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
               int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
               int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture))
            : null;
    }

    private static double ParseOpacity(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        Match match = Regex.Match(
            value,
            @"rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*([0-9.]+)\s*\)",
            RegexOptions.IgnoreCase);

        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return Math.Clamp(parsed, 0.1d, 1d);
        }

        return fallback;
    }

    private async Task SaveAsync()
    {
        if (User is null)
        {
            return;
        }

        saving = true;
        try
        {
            preferences[PreferenceKey] = JsonSerializer.Serialize(settings, SerializerOptions);
            await UserService.UpdateProfileAsync(User.Id, new Dictionary<string, string>(preferences, StringComparer.OrdinalIgnoreCase));
            initialSettings = settings.Clone();
            Snackbar.Add(Localizer["AccessibilitySaved"], Severity.Success);
        }
        catch (Exception)
        {
            Snackbar.Add(Localizer["AccessibilitySaveFailed"], Severity.Error);
        }
        finally
        {
            saving = false;
        }
    }

    private Task ResetAsync()
    {
        settings = initialSettings.Clone();
        ApplySettings();
        return InvokeAsync(StateHasChanged);
    }

    private static bool TryDeserialize(string json, out AccessibilitySettings? storedSettings)
    {
        try
        {
            storedSettings = JsonSerializer.Deserialize<AccessibilitySettings>(json, SerializerOptions);
            return storedSettings is not null;
        }
        catch (JsonException)
        {
            storedSettings = null;
            return false;
        }
    }

    private sealed class AccessibilitySettings
    {
        public double FontScale { get; set; } = 1d;

        public int FontWeight { get; set; } = 500;

        public double LineHeight { get; set; } = 1.5d;

        public double SurfaceOpacity { get; set; } = 0.65d;

        public double SurfaceBlur { get; set; } = 20d;

        public double Brightness { get; set; } = 100d;

        public bool HighContrast { get; set; }

        public AccessibilitySettings Clone() => new()
        {
            FontScale = FontScale,
            FontWeight = FontWeight,
            LineHeight = LineHeight,
            SurfaceOpacity = SurfaceOpacity,
            SurfaceBlur = SurfaceBlur,
            Brightness = Brightness,
            HighContrast = HighContrast
        };
    }
}
