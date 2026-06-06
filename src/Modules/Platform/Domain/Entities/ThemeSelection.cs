namespace Vitarish.Platform.Domain.Entities;

public sealed class ThemeSelection
{
    public int Id { get; set; }
    public Guid ActiveThemeId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
