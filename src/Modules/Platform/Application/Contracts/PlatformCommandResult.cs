namespace Vitarish.Platform.Application.Contracts;

public sealed record PlatformCommandResult(bool Succeeded, string? Error = null)
{
    public static PlatformCommandResult Success() => new(true);
    public static PlatformCommandResult Failure(string error) => new(false, error);
}

public sealed record CreateThemeResult(bool Succeeded, Guid? ThemeId = null, string? Error = null)
{
    public static CreateThemeResult Success(Guid themeId) => new(true, themeId);
    public static CreateThemeResult Failure(string error) => new(false, null, error);
}
