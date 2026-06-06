namespace Vitarish.Interfaces.Base.Abstractions;

public interface IPlatformThemePaletteProvider
{
    Task<PlatformThemePalette?> GetActivePaletteAsync(CancellationToken cancellationToken = default);
}
