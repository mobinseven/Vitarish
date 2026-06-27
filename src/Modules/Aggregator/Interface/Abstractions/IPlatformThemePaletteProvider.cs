namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface IPlatformThemePaletteProvider
{
    Task<PlatformThemePalette?> GetActivePaletteAsync(CancellationToken cancellationToken = default);
}
