namespace Vitarish.Modules.Base.BackgroundJobs;

/// <summary>
/// Carries request identity values required by background work running outside HTTP.
/// </summary>
public sealed record BackgroundRequestContext(
    string Authority,
    string? ClientId,
    string? OwnerId,
    string? TenantId,
    string? ResourceId,
    string? RequestId);
