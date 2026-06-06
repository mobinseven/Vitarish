namespace Vitarish.Platform.Domain.Models;

public sealed class ThemePalette
{
    public const string FallbackPrimary = "#2563EB";
    public const string FallbackPrimaryContrastText = "#FFFFFF";
    public const string FallbackSecondary = "#0EA5E9";
    public const string FallbackSecondaryContrastText = "#001018";
    public const string FallbackTertiary = "#A855F7";
    public const string FallbackTertiaryContrastText = "#FFFFFF";
    public const string FallbackInfo = "#0288D1";
    public const string FallbackInfoContrastText = "#FFFFFF";
    public const string FallbackSuccess = "#2E7D32";
    public const string FallbackSuccessContrastText = "#FFFFFF";
    public const string FallbackWarning = "#ED6C02";
    public const string FallbackWarningContrastText = "#FFFFFF";
    public const string FallbackError = "#D32F2F";
    public const string FallbackErrorContrastText = "#FFFFFF";
    public const string FallbackBackground = "#F1F1F1";
    public const string FallbackSurface = "#F1F1F1";
    public const string FallbackAppbarBackground = "#FFFFFF";
    public const string FallbackAppbarText = "#0F172A";
    public const string FallbackTextPrimary = "#0F172A";
    public const string FallbackTextSecondary = "#334155";
    public const string FallbackDivider = "#E2E8F0";

    public string Primary { get; set; } = FallbackPrimary;
    public string PrimaryContrastText { get; set; } = FallbackPrimaryContrastText;
    public string Secondary { get; set; } = FallbackSecondary;
    public string SecondaryContrastText { get; set; } = FallbackSecondaryContrastText;
    public string Tertiary { get; set; } = FallbackTertiary;
    public string TertiaryContrastText { get; set; } = FallbackTertiaryContrastText;
    public string Info { get; set; } = FallbackInfo;
    public string InfoContrastText { get; set; } = FallbackInfoContrastText;
    public string Success { get; set; } = FallbackSuccess;
    public string SuccessContrastText { get; set; } = FallbackSuccessContrastText;
    public string Warning { get; set; } = FallbackWarning;
    public string WarningContrastText { get; set; } = FallbackWarningContrastText;
    public string Error { get; set; } = FallbackError;
    public string ErrorContrastText { get; set; } = FallbackErrorContrastText;
    public string Background { get; set; } = FallbackBackground;
    public string Surface { get; set; } = FallbackSurface;
    public string AppbarBackground { get; set; } = FallbackAppbarBackground;
    public string AppbarText { get; set; } = FallbackAppbarText;
    public string TextPrimary { get; set; } = FallbackTextPrimary;
    public string TextSecondary { get; set; } = FallbackTextSecondary;
    public string Divider { get; set; } = FallbackDivider;
}
