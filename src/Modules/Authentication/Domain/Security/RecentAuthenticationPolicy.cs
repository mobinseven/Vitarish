namespace Vitarish.Authentication.Domain.Security;

public static class RecentAuthenticationPolicy
{
    public static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(10);

    public static bool IsRecent(DateTime? authenticatedAtUtc, DateTime nowUtc) =>
        authenticatedAtUtc is { Kind: DateTimeKind.Utc } authenticatedAt &&
        nowUtc.Kind is DateTimeKind.Utc &&
        authenticatedAt <= nowUtc &&
        nowUtc - authenticatedAt <= MaximumAge;
}
