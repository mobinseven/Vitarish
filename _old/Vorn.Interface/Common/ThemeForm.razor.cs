namespace Vorn.Interface.Common;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor.Utilities;
using Vorn.Interface.Services;

public partial class ThemeForm
{
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    [Parameter] public EventCallback<string> OnThemeChanged { get; set; }

    private ThemeFormModel model = default!;
    private ThemeFormModel initialModel = default!;
    private IReadOnlyList<KeyValuePair<string, string>> lightSurfaceVariables = Array.Empty<KeyValuePair<string, string>>();
    private IReadOnlyList<KeyValuePair<string, string>> darkSurfaceVariables = Array.Empty<KeyValuePair<string, string>>();
    private string typographyPreset = "default";

    private static readonly Dictionary<string, (string fontSize, double fontSizeValue, string fontWeight, double fontWeightValue)> TypographyPresets = new()
    {
        ["compact"] = ("0.875rem", 0.875, "400", 400),
        ["default"] = ("1rem", 1.0, "500", 500),
        ["comfortable"] = ("1.125rem", 1.125, "500", 500),
        ["large"] = ("1.25rem", 1.25, "500", 500)
    };

    private string LightSurfaceStyle =>
        $"background: {model.LightSurface ?? "transparent"}; border: {model.LightBorder ?? "none"}; " +
        $"color: {model.LightTextPrimary ?? "inherit"}; " +
        $"backdrop-filter: blur({model.LightBlurCss}); filter: brightness({model.LightBrightnessCss});";

    private string DarkSurfaceStyle =>
        $"background: {model.DarkBackground ?? "transparent"}; border: {model.DarkBorder ?? "none"}; " +
        $"backdrop-filter: blur({model.DarkBlurCss}); filter: brightness({model.DarkBrightnessCss}); color: {model.DarkTextPrimary ?? "inherit"};";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        initialModel = ThemeFormModel.FromTheme(ThemeService);
        model = initialModel.Clone();
        UpdateSurfaceSnapshots();
        DetectTypographyPreset();
    }

    private void DetectTypographyPreset()
    {
        var currentSize = ThemeService.Typography.FontSize;
        var currentWeight = ThemeService.Typography.FontWeight;
        
        foreach (var (presetName, presetValues) in TypographyPresets)
        {
            if (presetValues.fontSize == currentSize && presetValues.fontWeight == currentWeight)
            {
                typographyPreset = presetName;
                return;
            }
        }
        
        typographyPreset = "custom";
    }

    private void UpdateSurfaceSnapshots()
    {
        lightSurfaceVariables = ThemeService.Light.RootVariables.ToArray();
        darkSurfaceVariables = ThemeService.Dark.RootVariables.ToArray();
    }

    private Task ResetToInitial()
    {
        model = initialModel.Clone();
        ApplyThemeChanges();

        return Task.CompletedTask;
    }

    private void ApplyThemeChanges()
    {
        ThemeService.UpdateLightPaletteColors(
            primary: model.LightPrimary,
            secondary: model.LightSecondary,
            tertiary: model.LightTertiary,
            info: model.LightInfo,
            success: model.LightSuccess,
            warning: model.LightWarning,
            error: model.LightError,
            dark: model.LightDarkColor,
            surface: model.LightSurface,
            textPrimary: model.LightTextPrimary);

        ThemeService.UpdateDarkPaletteColors(
            primary: model.DarkPrimary,
            secondary: model.DarkSecondary,
            tertiary: model.DarkTertiary,
            info: model.DarkInfo,
            success: model.DarkSuccess,
            warning: model.DarkWarning,
            error: model.DarkError,
            dark: model.DarkDarkColor,
            surface: model.DarkSurface,
            textPrimary: model.DarkTextPrimary);

        ThemeService.UpdateTypography(
            model.FontSize,
            model.FontWeight);

        ThemeService.UpdateLightSurface(
            model.LightBlurCss,
            model.LightBrightnessCss,
            model.LightBackground,
            model.LightBorder);

        ThemeService.UpdateDarkSurface(
            model.DarkBlurCss,
            model.DarkBrightnessCss,
            model.DarkBackground,
            model.DarkBorder);

        ThemeService.SetDarkMode(model.DarkMode);
        ThemeService.SetBlazorReadyEnabled(model.BlazorReady);

        model = ThemeFormModel.FromTheme(ThemeService);
        UpdateSurfaceSnapshots();

        _ = InvokeAsync(StateHasChanged);
    }

    private Task OnFontSizeSliderChanged()
    {
        model.FontSize = $"{model.FontSizeValue.ToString("0.##", CultureInfo.InvariantCulture)}rem";
        typographyPreset = "custom";
        return ApplyAfterBindingAsync();
    }

    private Task OnFontWeightSliderChanged()
    {
        var roundedWeight = (int)(Math.Round(model.FontWeightValue / 100d, MidpointRounding.AwayFromZero) * 100d);
        model.FontWeight = roundedWeight.ToString(CultureInfo.InvariantCulture);
        model.FontWeightValue = roundedWeight;
        typographyPreset = "custom";
        return ApplyAfterBindingAsync();
    }

    private Task OnTypographyPresetChanged(string preset)
    {
        typographyPreset = preset;
        
        if (preset != "custom" && TypographyPresets.TryGetValue(preset, out var presetValues))
        {
            model.FontSize = presetValues.fontSize;
            model.FontSizeValue = presetValues.fontSizeValue;
            model.FontWeight = presetValues.fontWeight;
            model.FontWeightValue = presetValues.fontWeightValue;
        }
        // If "custom", keep current values
        
        return ApplyAfterBindingAsync();
    }

    private async Task ApplyAfterBindingAsync()
    {
        ApplyThemeChanges();
        if(OnThemeChanged.HasDelegate)
        {
            await OnThemeChanged.InvokeAsync(ThemeService.GetThemeSettingsJson());
        }
    }

    private sealed class ThemeFormModel
    {
        public string? LightPrimary { get; set; }
        public string? LightSecondary { get; set; }
        public string? LightTertiary { get; set; }
        public string? LightInfo { get; set; }
        public string? LightTextPrimary { get; set; }
        public string? LightSuccess { get; set; }
        public string? LightWarning { get; set; }
        public string? LightError { get; set; }
        public string? LightDarkColor { get; set; }
        public string? LightSurface { get; set; }
        public string? DarkPrimary { get; set; }
        public string? DarkSecondary { get; set; }
        public string? DarkTertiary { get; set; }
        public string? DarkInfo { get; set; }
        public string? DarkTextPrimary { get; set; }
        public string? DarkSuccess { get; set; }
        public string? DarkWarning { get; set; }
        public string? DarkError { get; set; }
        public string? DarkDarkColor { get; set; }
        public string? DarkSurface { get; set; }
        public string? FontSize { get; set; }
        public string? FontWeight { get; set; }
        public double FontSizeValue { get; set; }
        public double FontWeightValue { get; set; }
        public double LightBlur { get; set; }
        public double LightBrightness { get; set; }
        public string? LightBackground { get; set; }
        public string? LightBorder { get; set; }
        public double DarkBlur { get; set; }
        public double DarkBrightness { get; set; }
        public string? DarkBackground { get; set; }
        public string? DarkBorder { get; set; }
        public bool DarkMode { get; set; }
        public bool BlazorReady { get; set; }

        public string LightBlurCss => $"{LightBlur.ToString(CultureInfo.InvariantCulture)}px";

        public string LightBrightnessCss => $"{LightBrightness.ToString(CultureInfo.InvariantCulture)}%";

        public string DarkBlurCss => $"{DarkBlur.ToString(CultureInfo.InvariantCulture)}px";

        public string DarkBrightnessCss => $"{DarkBrightness.ToString(CultureInfo.InvariantCulture)}%";

        public ThemeFormModel Clone() => new()
        {
            LightPrimary = LightPrimary,
            LightSecondary = LightSecondary,
            LightTertiary = LightTertiary,
            LightInfo = LightInfo,
            LightTextPrimary = LightTextPrimary,
            LightSuccess = LightSuccess,
            LightWarning = LightWarning,
            LightError = LightError,
            LightDarkColor = LightDarkColor,
            LightSurface = LightSurface,
            DarkPrimary = DarkPrimary,
            DarkSecondary = DarkSecondary,
            DarkTertiary = DarkTertiary,
            DarkInfo = DarkInfo,
            DarkTextPrimary = DarkTextPrimary,
            DarkSuccess = DarkSuccess,
            DarkWarning = DarkWarning,
            DarkError = DarkError,
            DarkDarkColor = DarkDarkColor,
            DarkSurface = DarkSurface,
            FontSize = FontSize,
            FontWeight = FontWeight,
            FontSizeValue = FontSizeValue,
            FontWeightValue = FontWeightValue,
            LightBlur = LightBlur,
            LightBrightness = LightBrightness,
            LightBackground = LightBackground,
            LightBorder = LightBorder,
            DarkBlur = DarkBlur,
            DarkBrightness = DarkBrightness,
            DarkBackground = DarkBackground,
            DarkBorder = DarkBorder,
            DarkMode = DarkMode,
            BlazorReady = BlazorReady,
        };

        public static ThemeFormModel FromTheme(IThemeService themeService)
        {
            var theme = themeService.Theme;
            var typography = theme.Typography;
            var defaultTypography = typography.Default;

            return new ThemeFormModel
            {
                LightPrimary = theme.PaletteLight?.Primary?.Value,
                LightSecondary = theme.PaletteLight?.Secondary?.Value,
                LightTertiary = theme.PaletteLight?.Tertiary?.Value,
                LightInfo = theme.PaletteLight?.Info?.Value,
                LightTextPrimary = theme.PaletteLight?.TextPrimary?.Value,
                LightSuccess = theme.PaletteLight?.Success?.Value,
                LightWarning = theme.PaletteLight?.Warning?.Value,
                LightError = theme.PaletteLight?.Error?.Value,
                LightDarkColor = theme.PaletteLight?.Dark?.Value,
                LightSurface = theme.PaletteLight?.Surface?.Value,
                DarkPrimary = theme.PaletteDark?.Primary?.Value,
                DarkSecondary = theme.PaletteDark?.Secondary?.Value,
                DarkTertiary = theme.PaletteDark?.Tertiary?.Value,
                DarkInfo = theme.PaletteDark?.Info?.Value,
                DarkTextPrimary = theme.PaletteDark?.TextPrimary?.Value,
                DarkSuccess = theme.PaletteDark?.Success?.Value,
                DarkWarning = theme.PaletteDark?.Warning?.Value,
                DarkError = theme.PaletteDark?.Error?.Value,
                DarkDarkColor = theme.PaletteDark?.Dark?.Value,
                DarkSurface = theme.PaletteDark?.Surface?.Value,
                FontSize = defaultTypography?.FontSize,
                FontWeight = defaultTypography?.FontWeight,
                FontSizeValue = ParseCssNumber(defaultTypography?.FontSize, "rem", 1d),
                FontWeightValue = ParseCssNumber(defaultTypography?.FontWeight, string.Empty, 500d),
                LightBlur = ParseCssNumber(themeService.Light.Blur, "px", 0d),
                LightBrightness = ParseCssNumber(themeService.Light.Brightness, "%", 100d),
                LightBackground = themeService.Light.Background,
                LightBorder = themeService.Light.Border,
                DarkBlur = ParseCssNumber(themeService.Dark.Blur, "px", 0d),
                DarkBrightness = ParseCssNumber(themeService.Dark.Brightness, "%", 100d),
                DarkBackground = themeService.Dark.Background,
                DarkBorder = themeService.Dark.Border,
                DarkMode = themeService.DarkModeEnabled,
                BlazorReady = themeService.BlazorReadyEnabled,
            };
        }

        private static double ParseCssNumber(string? value, string suffix, double fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var trimmed = value.Trim();

            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
            }

            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return fallback;
        }
    }
}
