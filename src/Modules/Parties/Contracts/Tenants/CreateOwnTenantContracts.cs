namespace Vitarish.Parties.Contracts.Tenants;

/// <summary>A signed-in user's request to create their own clinic; the creator comes from the session.</summary>
public sealed record CreateOwnTenantRequest(string Name, string Subdomain, string? Description);

public sealed record CreateOwnTenantResponse(Guid TenantId, string Hostname);
