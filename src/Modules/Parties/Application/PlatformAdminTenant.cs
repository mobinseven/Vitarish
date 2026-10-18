namespace Vitarish.Parties.Application;

/// <summary>
/// The platform administration app's own tenant. It holds no practice data; it
/// exists so sign-in on the admin host has a tenant to bind sessions to, as the
/// landing host does.
/// </summary>
public static class PlatformAdminTenant
{
    public const string OwnerName = "Vitarish Platform";
    public const string AppId = "vitarish-platform-admin";
    public const string Name = "Vitarish Platform Administration";
}

/// <summary>
/// The admin app's public URL (<c>Vitarish:PlatformAdminUrl</c>). When set, startup
/// binds its host to <see cref="PlatformAdminTenant"/>.
/// </summary>
public sealed class PlatformAdminHostOptions
{
    public string? PlatformAdminUrl { get; set; }
}
