namespace Vitarish.Platform.Domain.Entities;

public sealed class ThemePalette
{
    public string Primary { get; set; } = string.Empty;
    public string PrimaryContrastText { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string SecondaryContrastText { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string AppbarBackground { get; set; } = string.Empty;
    public string AppbarText { get; set; } = string.Empty;
    public string TextPrimary { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string Divider { get; set; } = string.Empty;
}
