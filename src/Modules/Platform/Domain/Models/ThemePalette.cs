namespace Vitarish.Platform.Domain.Models;

public sealed class ThemePalette
{
    public const string FallbackPrimary = "#2563EB";
    public const string FallbackPrimaryContrastText = "#FFFFFF";
    public const string FallbackSecondary = "#0EA5E9";
    public const string FallbackSecondaryContrastText = "#001018";
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
    public string Background { get; set; } = FallbackBackground;
    public string Surface { get; set; } = FallbackSurface;
    public string AppbarBackground { get; set; } = FallbackAppbarBackground;
    public string AppbarText { get; set; } = FallbackAppbarText;
    public string TextPrimary { get; set; } = FallbackTextPrimary;
    public string TextSecondary { get; set; } = FallbackTextSecondary;
    public string Divider { get; set; } = FallbackDivider;
}
