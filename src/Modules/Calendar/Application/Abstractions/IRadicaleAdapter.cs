namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Sole CalDAV boundary to the Radicale server. All CalDAV I/O passes through this interface.
/// </summary>
public interface IRadicaleAdapter
{
    Task CreateCollectionAsync(string principalPath, string slug, string displayName, string timeZone, CancellationToken ct);

    Task DeleteCollectionAsync(string collectionUrl, CancellationToken ct);

    /// <returns>The new ETag assigned by Radicale.</returns>
    Task<string> PutCalendarObjectAsync(string collectionUrl, string objectName, string iCalData, string? ifMatchETag, CancellationToken ct);

    Task<(string iCalData, string eTag)> GetCalendarObjectAsync(string resourceUrl, CancellationToken ct);

    Task DeleteCalendarObjectAsync(string resourceUrl, string? ifMatchETag, CancellationToken ct);

    Task<IReadOnlyList<(string url, string iCalData, string eTag)>> QueryTimeRangeAsync(string collectionUrl, TimeRangeQuery range, CancellationToken ct);

    Task<string?> GetETagAsync(string resourceUrl, CancellationToken ct);

    Task<IReadOnlyList<(string url, string eTag)>> ListCollectionObjectsAsync(string collectionUrl, CancellationToken ct);
}
