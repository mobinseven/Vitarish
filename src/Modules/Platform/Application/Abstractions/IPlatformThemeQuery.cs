using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformThemeQuery
{
    Task<IReadOnlyList<ThemeListItem>> ListThemesAsync(CancellationToken cancellationToken = default);
    Task<ThemeEditorModel?> GetThemeAsync(Guid themeId, CancellationToken cancellationToken = default);
    Task<ThemeEditorModel?> GetActiveThemeAsync(CancellationToken cancellationToken = default);
}
