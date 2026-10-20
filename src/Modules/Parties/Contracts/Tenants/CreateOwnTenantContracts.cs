namespace Vitarish.Parties.Contracts.Tenants;

/// <summary>A signed-in user's request to create their own clinic; the creator comes from the session.</summary>
/// <param name="Category">A code from the onboarding categories list.</param>
/// <param name="BusinessDetails">Optional public listing details; photos are added after creation.</param>
public sealed record CreateOwnTenantRequest(
    string Name,
    string Subdomain,
    string? Description,
    string? Category = null,
    TenantBusinessDetailsDto? BusinessDetails = null);

/// <param name="HandoffToken">One-time token, valid for a minute on <paramref name="Hostname"/> only, to continue there signed in; null when the creator must sign in there.</param>
public sealed record CreateOwnTenantResponse(Guid TenantId, string Hostname, string? HandoffToken = null);
