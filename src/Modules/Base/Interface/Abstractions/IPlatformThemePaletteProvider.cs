namespace Vitarish.Modules.Base.Interface.Abstractions;

public interface IPlatformThemePaletteProvider
{
    Task<PlatformThemePalette?> GetActivePaletteAsync(CancellationToken cancellationToken = default);
}
