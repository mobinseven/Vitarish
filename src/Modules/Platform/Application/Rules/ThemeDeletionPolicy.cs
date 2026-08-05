namespace Vitarish.Platform.Application.Rules;

[Obsolete("Platform theme administration is obsolete and is no longer exposed by platform-admin.")]
public static class ThemeDeletionPolicy
{
    public const string ActiveThemeDeleteError = "Active theme cannot be deleted.";

    public static string? ValidateInactiveDeletion(Guid themeId, Guid? activeThemeId)
        => activeThemeId == themeId ? ActiveThemeDeleteError : null;
}
