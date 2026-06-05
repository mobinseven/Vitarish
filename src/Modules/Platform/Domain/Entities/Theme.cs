namespace Vitarish.Platform.Domain.Entities;

public sealed class Theme
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ThemePalette LightPalette { get; set; } = new();
    public ThemePalette DarkPalette { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
