using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Application.Contracts;

public sealed record ThemeWriteModel(
    string Name,
    ThemePalette LightPalette,
    ThemePalette DarkPalette);
