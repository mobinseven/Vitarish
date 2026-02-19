using System.Text.Json;

namespace Vitarish.Instagram.Scraper;

public sealed record DatasetItemsResponse(IReadOnlyList<JsonElement> Items, JsonElement Raw)
{
    public static async Task<DatasetItemsResponse> FromStreamAsync(Stream? stream, CancellationToken ct)
    {
        if (stream is null)
        {
            throw new InvalidOperationException("Instagram scraper did not return dataset items.");
        }

        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        JsonElement root = document.RootElement.Clone();

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Dataset items response is not a JSON array.");
        }

        JsonElement[] items = [.. root.EnumerateArray()];
        return new DatasetItemsResponse(items, root);
    }
}
