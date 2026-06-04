namespace Vitarish.Links.Domain.Entities;

/// <summary>
/// Represents a short link that redirects visitors to a destination URL.
/// The (Domain, Slug) pair is globally unique across all tenants.
/// </summary>
public class Link : Entity
{
    /// <summary>Owning tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Normalized lowercase hostname this link is served from (e.g. "example.com").</summary>
    public required string Domain { get; set; } = string.Empty;

    /// <summary>URL-safe short code (e.g. "aB3xY7qZ").</summary>
    public required string Slug { get; set; } = string.Empty;

    /// <summary>Validated absolute HTTP or HTTPS URL visitors are redirected to.</summary>
    public required string DestinationUrl { get; set; } = string.Empty;

    /// <summary>Whether the link is active or manually disabled.</summary>
    public LinkStatus Status { get; set; } = LinkStatus.Active;

    /// <summary>Optional expiry; null means the link never expires.</summary>
    public DateTime? ExpiresAt { get; set; }
}
