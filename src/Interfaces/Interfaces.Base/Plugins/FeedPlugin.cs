using System.Text.Json;
using Vitarish.Interfaces.Base.Common;
namespace Vitarish.Interfaces.Base.Plugins;

public sealed record FeedPlugin(
    IReadOnlyDictionary<string, object?> Metadata,
    int Take = 5) : PluginContext(Metadata);

public interface IFeedPlugin : IPlugin<FeedPlugin>;

public static class FeedPluginConfiguration
{
    public const string SourceIdsMetadataKey = "feedSources";

    public static IReadOnlyList<string> GetConfiguredSourceIds(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue(SourceIdsMetadataKey, out object? value))
        {
            return [];
        }

        return value switch
        {
            string source => [.. source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
            IEnumerable<string> sourceIds => [.. sourceIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)],
            JsonElement jsonElement => ParseJsonElement(jsonElement),
            IEnumerable<object?> sourceItems => [.. sourceItems
                .OfType<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)],
            _ => []
        };
    }

    private static IReadOnlyList<string> ParseJsonElement(JsonElement jsonElement) => jsonElement.ValueKind switch
    {
        JsonValueKind.String => GetConfiguredSourceIds(new Dictionary<string, object?> { [SourceIdsMetadataKey] = jsonElement.GetString() }),
        JsonValueKind.Array => [.. jsonElement
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .OfType<string>()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)],
        _ => []
    };
}
