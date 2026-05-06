namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Pagination and filtering options for a search operation.
/// </summary>
public record SearchOptions
{
    /// <summary>
    /// Default number of documents to return when a non-positive limit is supplied.
    /// </summary>
    public const int DefaultLimit = 20;

    /// <summary>
    /// Maximum number of documents to return in a single search request.
    /// </summary>
    public const int MaxLimit = 100;

    /// <summary>
    /// Number of documents to skip (for pagination).
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Maximum number of documents to return.
    /// </summary>
    public int Limit { get; init; } = DefaultLimit;

    /// <summary>
    /// Optional document type filter (e.g. "project").
    /// When null, all types are returned.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Returns a copy with safe pagination values applied.
    /// </summary>
    public SearchOptions Normalize() => this with
    {
        Skip = Math.Max(0, Skip),
        Limit = NormalizeLimit(Limit)
    };

    /// <summary>
    /// Applies the shared search page-size rules to <paramref name="limit"/>.
    /// </summary>
    public static int NormalizeLimit(int limit) => limit <= 0
        ? DefaultLimit
        : Math.Min(limit, MaxLimit);
}
