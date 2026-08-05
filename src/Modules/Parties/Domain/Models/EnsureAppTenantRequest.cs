namespace Vitarish.Parties.Domain.Models;

public sealed record EnsureAppTenantRequest(
    string OwnerName,
    string AppId,
    string Name,
    string Hostname);
