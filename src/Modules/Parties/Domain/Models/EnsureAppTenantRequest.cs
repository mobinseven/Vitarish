namespace Vitarish.Parties.Domain.Models;

public sealed record EnsureAppTenantRequest(
    Guid OwnerId,
    Guid TenantId,
    Guid PrimaryIndicatorId,
    string AppId,
    string Name,
    string Hostname);
