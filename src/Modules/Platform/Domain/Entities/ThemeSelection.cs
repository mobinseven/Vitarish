namespace Vitarish.Platform.Domain.Entities;

[Obsolete("Platform theme persistence is obsolete; retain existing data for compatibility only.")]
public sealed class ThemeSelection
{
    public int Id { get; set; }
    public Guid ActiveThemeId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
