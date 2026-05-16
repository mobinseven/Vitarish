using System.Text.Json;

namespace Vitarish.Parties.Domain;

public static class TenantMetadata
{
    public const string ActiveModulesKey = "activeModules";

    public static IReadOnlyList<string> GetActiveModules(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue(ActiveModulesKey, out object? raw) || raw is null)
        {
            return [];
        }

        try
        {
            return raw switch
            {
                JsonElement element => element.Deserialize<string[]>() ?? [],
                string[] modules => modules,
                IEnumerable<string> modules => [.. modules],
                System.Collections.IEnumerable modules => [.. modules.OfType<string>()],
                _ => []
            };
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void SetActiveModules(IDictionary<string, object?> metadata, IEnumerable<string> modules)
    {
        metadata[ActiveModulesKey] = modules
            .Where(module => !string.IsNullOrWhiteSpace(module))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
