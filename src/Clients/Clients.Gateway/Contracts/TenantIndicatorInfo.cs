namespace Vitarish.Clients.Gateway.Contracts;

/// <summary>
/// Represents a tenant indicator (hostname/subdomain).
/// </summary>
public sealed record TenantIndicatorInfo
{
    /// <summary>Method (hostname or subdomain).</summary>
    public required string Method { get; init; }

    /// <summary>Canonical value (normalized hostname).</summary>
    public required string Value { get; init; }

    /// <summary>Current status.</summary>
    public required string Status { get; init; }
}
