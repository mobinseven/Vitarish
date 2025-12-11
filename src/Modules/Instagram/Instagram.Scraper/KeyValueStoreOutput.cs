using System.Text.Json;

namespace Vitarish.Instagram.Scraper;

public sealed record KeyValueStoreOutput(JsonElement Data)
{
    public static async Task<KeyValueStoreOutput> FromStreamAsync(Stream? stream, CancellationToken ct)
    {
        if (stream is null)
        {
            throw new InvalidOperationException("Instagram scraper did not return any key-value store output.");
        }

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var root = document.RootElement.Clone();
        return new KeyValueStoreOutput(root);
    }
}
