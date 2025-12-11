using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using Vitarish.Modules.Instagram.Scraper.Models;

namespace Vitarish.Instagram.Scraper;

public sealed class InstagramScraperService(IInstagramScraperApiClient client, ILogger<InstagramScraperService> logger) : IInstagramScraperService
{
    private readonly IInstagramScraperApiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger<InstagramScraperService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const long MillisecondsTimestampThreshold = 10_000_000_000; // Heuristic threshold to detect millisecond Unix timestamps.
    public async Task<InstagramData[]> FetchAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        InputSchema input = MapDescriptor(descriptor);
        DatasetItemsResponse dataset = await _client.RunScraperAndGetDatasetItemsAsync(input, cancellationToken).ConfigureAwait(false);
        List<InstagramData> mappedItems = new();
        foreach (JsonElement item in dataset.Items)
        {
            InstagramData? mappedItem = MapItem(descriptor.DataType, item);
            if (mappedItem is not null && ShouldInclude(mappedItem.PublishedAt, descriptor))
            {
                mappedItems.Add(mappedItem);
            }
        }
        InstagramData[] mapped = [.. mappedItems];

        return mapped;
    }

    public async Task<InstagramRunInfo> StartRunAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        InputSchema input = MapDescriptor(descriptor);
        RunInfoResponse response = await _client.StartScraperRunAsync(input, cancellationToken).ConfigureAwait(false);
        return new InstagramRunInfo(response.RunId, response.DefaultDatasetId, response.Run);
    }

    public async Task<InstagramRawOutput> RunSyncAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        InputSchema input = MapDescriptor(descriptor);
        KeyValueStoreOutput output = await _client.RunScraperSyncAsync(input, cancellationToken).ConfigureAwait(false);
        return new InstagramRawOutput(output.Data);
    }

    private static InputSchema MapDescriptor(InstagramDataDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        IReadOnlyCollection<string> directUrls = descriptor.DirectUrls ?? Array.Empty<string>();
        bool hasUrls = directUrls.Count > 0;
        bool hasSearch = !string.IsNullOrWhiteSpace(descriptor.SearchQuery);

        if (!hasUrls && !hasSearch)
        {
            throw new ArgumentException("Either DirectUrls or SearchQuery must be provided.", nameof(descriptor));
        }

        InputSchema input = new()
        {
            DirectUrls = hasUrls ? directUrls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList() : null,
            ResultsType = MapResultsType(descriptor.DataType),
            ResultsLimit = descriptor.MaxResultsPerUrl,
            Search = descriptor.SearchQuery,
        };

        if (descriptor.FromPublishedAt is { } from)
        {
            input.OnlyPostsNewerThan = from.ToString("O", CultureInfo.InvariantCulture);
        }

        IEnumerable<KeyValuePair<string, object?>> extraOptions = descriptor.ExtraOptions ?? new Dictionary<string, object?>();
        foreach ((string key, object? value) in extraOptions)
        {
            if (string.IsNullOrWhiteSpace(key) || input.AdditionalData.ContainsKey(key))
            {
                continue;
            }

            input.AdditionalData[key] = value;
        }

        return input;
    }

    private static InputSchema_resultsType MapResultsType(InstagramDataType dataType) => dataType switch
    {
        InstagramDataType.Posts => InputSchema_resultsType.Posts,
        InstagramDataType.Comments => InputSchema_resultsType.Comments,
        InstagramDataType.Details => InputSchema_resultsType.Details,
        InstagramDataType.Mentions => InputSchema_resultsType.Mentions,
        InstagramDataType.Reels => InputSchema_resultsType.Reels,
        InstagramDataType.Stories => InputSchema_resultsType.Stories,
        _ => InputSchema_resultsType.Posts,
    };

    private InstagramData MapItem(InstagramDataType dataType, JsonElement item)
    {
        string? id = ReadString(item, "id") ?? ReadString(item, "shortcode") ?? ReadString(item, "shortCode");
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Skipping Instagram dataset item without an identifier: {Item}", item);
            throw new InvalidOperationException("Instagram dataset item is missing an identifier.");
        }

        return new InstagramData
        {
            Id = id,
            DataType = dataType,
            Url = ReadString(item, "url") ?? ReadString(item, "postUrl") ?? string.Empty,
            AuthorUsername = ReadString(item, "ownerUsername") ?? ReadString(item, "username") ?? ReadString(item, "authorUsername"),
            Caption = ReadString(item, "caption") ?? ReadString(item, "text") ?? ReadString(item, "description"),
            PublishedAt = ReadTimestamp(item),
            LikeCount = ReadInt(item, "likesCount", "likeCount", "likes"),
            CommentCount = ReadInt(item, "commentsCount", "commentCount", "comments"),
            ViewCount = ReadInt(item, "playCount", "viewCount", "views"),
            Raw = item,
        };
    }

    private static bool ShouldInclude(DateTimeOffset? publishedAt, InstagramDataDescriptor descriptor)
    {
        if (descriptor.FromPublishedAt is null && descriptor.ToPublishedAt is null)
        {
            return true;
        }

        if (publishedAt is null)
        {
            return false;
        }

        if (descriptor.FromPublishedAt is { } from && publishedAt < from)
        {
            return false;
        }

        if (descriptor.ToPublishedAt is { } to && publishedAt > to)
        {
            return false;
        }

        return true;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static int? ReadInt(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int intValue))
            {
                return intValue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out long longValue))
            {
                return longValue >= int.MinValue && longValue <= int.MaxValue ? (int)longValue : null;
            }
        }

        return null;
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement element)
    {
        if (TryReadDate(element, "publishedAt", out DateTimeOffset publishedAt))
        {
            return publishedAt;
        }

        if (TryReadUnixDate(element, "timestamp", out DateTimeOffset timestamp))
        {
            return timestamp;
        }

        if (TryReadUnixDate(element, "takenAtTimestamp", out DateTimeOffset takenAtTimestamp))
        {
            return takenAtTimestamp;
        }

        if (TryReadDate(element, "takenAt", out DateTimeOffset takenAt))
        {
            return takenAt;
        }

        return null;
    }

    private static bool TryReadUnixDate(JsonElement element, string propertyName, out DateTimeOffset date)
    {
        date = default;
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        if (!property.TryGetInt64(out long rawValue))
        {
            return false;
        }

        try
        {
            date = rawValue > MillisecondsTimestampThreshold
                ? DateTimeOffset.FromUnixTimeMilliseconds(rawValue)
                : DateTimeOffset.FromUnixTimeSeconds(rawValue);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryReadDate(JsonElement element, string propertyName, out DateTimeOffset date)
    {
        date = default;
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return DateTimeOffset.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date);
    }
}
