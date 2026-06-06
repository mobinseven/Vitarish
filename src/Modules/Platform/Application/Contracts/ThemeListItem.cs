using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemeListItem(
    Guid Id,
    string Name,
    ThemePalette LightPalette,
    ThemePalette DarkPalette,
    bool IsActive);
