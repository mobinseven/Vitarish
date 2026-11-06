namespace Vitarish.Parties.Application.Apps;

/// <summary>Whether an app is available to create or is announced for a later release.</summary>
public enum PlatformAppAvailability
{
    Live,
    Soon
}

/// <summary>An app hosted by the platform and the workspace options it supports.</summary>
public sealed record PlatformApp(
    string AppId,
    string Name,
    PlatformAppAvailability Availability,
    bool HostsWorkspaces,
    Onboarding.SelfServiceTenantProfile? SelfService);

/// <summary>Read-only view of apps registered by the platform's app composition.</summary>
public interface IPlatformAppCatalog
{
    IReadOnlyList<PlatformApp> Apps { get; }
}

/// <summary>Collects app registrations into the shared platform catalog.</summary>
public sealed class PlatformAppCatalog(IEnumerable<PlatformApp> apps) : IPlatformAppCatalog
{
    public IReadOnlyList<PlatformApp> Apps { get; } = Array.AsReadOnly(apps.ToArray());
}
