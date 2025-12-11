namespace Vitarish.Clients.Identity.Contracts;

/// <summary>
/// Represents compact tenant information for client use.
/// </summary>
public sealed record TenantInfo
{
    /// <summary>Unique tenant identifier.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Owner identifier.</summary>
    public required Guid OwnerId { get; init; }

    /// <summary>Canonical key (URL-safe identifier).</summary>
    public required string CanonicalKey { get; init; }

    /// <summary>Primary hostname for this tenant.</summary>
    public required string PrimaryHostname { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Logo URL (nullable).</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Authentication client ID (nullable).</summary>
    public string? AuthClientId { get; init; }

    /// <summary>Active tenant indicators.</summary>
    public IReadOnlyList<TenantIndicatorInfo> Indicators { get; init; } = [];
}
