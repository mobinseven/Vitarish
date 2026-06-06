namespace Vitarish.Platform.Application.Rules;

public static class ThemeDeletionPolicy
{
    public const string ActiveThemeDeleteError = "Active theme cannot be deleted.";

    public static string? ValidateInactiveDeletion(Guid themeId, Guid? activeThemeId)
        => activeThemeId == themeId ? ActiveThemeDeleteError : null;
}
