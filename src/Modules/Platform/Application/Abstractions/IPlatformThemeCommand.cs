using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformThemeCommand
{
    Task<CreateThemeResult> CreateThemeAsync(ThemeWriteModel model, CancellationToken cancellationToken = default);
    Task<PlatformCommandResult> UpdateThemeAsync(Guid themeId, ThemeWriteModel model, CancellationToken cancellationToken = default);
    Task<PlatformCommandResult> SetActiveThemeAsync(Guid themeId, CancellationToken cancellationToken = default);
    Task<PlatformCommandResult> DeleteInactiveThemeAsync(Guid themeId, CancellationToken cancellationToken = default);
}
