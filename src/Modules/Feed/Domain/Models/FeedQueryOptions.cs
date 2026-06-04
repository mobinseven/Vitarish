namespace Vitarish.Feed.Domain.Models;

/// <summary>
/// Configuration options for feed fetching and filtering
/// </summary>
public record FeedQueryOptions
{
    /// <summary>
    /// Maximum number of items to return
    /// </summary>
    public int Limit { get; init; } = 50;

    /// <summary>
    /// Number of items to skip (for pagination)
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Filter by specific source IDs (null = all sources)
    /// </summary>
    public IReadOnlyList<string>? SourceIds { get; init; }

    /// <summary>
    /// Filter by item types (null = all types)
    /// </summary>
    public IReadOnlyList<string>? ItemTypes { get; init; }

    /// <summary>
    /// Only include items after this timestamp
    /// </summary>
    public DateTime? After { get; init; }

    /// <summary>
    /// Only include items before this timestamp
    /// </summary>
    public DateTime? Before { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant filtering (null = current tenant)
    /// </summary>
    public Guid? TenantId { get; init; }
}
