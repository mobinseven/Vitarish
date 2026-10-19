using System.Text.Json.Serialization;

namespace Vitarish.Parties.Contracts.Tenants;

[JsonConverter(typeof(JsonStringEnumConverter<TenantSubdomainAvailabilityStatus>))]
public enum TenantSubdomainAvailabilityStatus { Available = 1, Malformed = 2, Reserved = 3, Taken = 4 }

/// <summary>Whether a subdomain can become a new tenant's address; <see cref="Hostname"/> is set when it is well formed and not reserved.</summary>
public sealed record TenantSubdomainAvailabilityResponse(string Subdomain, string? Hostname, TenantSubdomainAvailabilityStatus Status);
