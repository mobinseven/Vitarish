namespace Vitarish.Authentication.Domain.Security;

/// <summary>
/// Bounds and default for the administrator-configurable idle expiry of a browser session
/// (NSMAIL-1 FTA_SSL.3.1). The same bounds apply to the global default and to per-user overrides.
/// </summary>
public static class SessionInactivityTimeout
{
    public const int MinimumMinutes = 1;
    public const int MaximumMinutes = 90 * 24 * 60;
    public const int DefaultMinutes = 14 * 24 * 60;

    public static bool IsWithinBounds(int minutes) => minutes is >= MinimumMinutes and <= MaximumMinutes;

    public static ArgumentOutOfRangeException OutOfBounds(string paramName) =>
        new(paramName, $"The inactivity timeout must be between {MinimumMinutes} and {MaximumMinutes} minutes.");
}
