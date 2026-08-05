using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

[Obsolete("Platform theme administration is obsolete and is no longer exposed by platform-admin.")]
public interface IPlatformThemeQuery
{
    Task<IReadOnlyList<ThemeListItem>> ListThemesAsync(CancellationToken cancellationToken = default);
    Task<ThemeEditorModel?> GetThemeAsync(Guid themeId, CancellationToken cancellationToken = default);
    Task<ThemeEditorModel?> GetActiveThemeAsync(CancellationToken cancellationToken = default);
}
