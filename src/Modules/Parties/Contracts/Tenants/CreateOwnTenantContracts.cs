namespace Vitarish.Parties.Contracts.Tenants;

/// <summary>A signed-in user's request to create their own clinic; the creator comes from the session.</summary>
public sealed record CreateOwnTenantRequest(string Name, string Subdomain, string? Description);

/// <param name="HandoffToken">One-time token, valid for a minute on <paramref name="Hostname"/> only, to continue there signed in; null when the creator must sign in there.</param>
public sealed record CreateOwnTenantResponse(Guid TenantId, string Hostname, string? HandoffToken = null);
