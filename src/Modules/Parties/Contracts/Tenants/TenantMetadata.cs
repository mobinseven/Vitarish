using System.Text.Json;
namespace Vitarish.Parties.Contracts.Tenants;
public static class TenantMetadata
{
    public const string ActiveModulesKey = "activeModules";
    public const string ActiveAppsKey = "activeApps";
    public static IReadOnlyList<string> GetActiveModules(IReadOnlyDictionary<string, object?>? metadata) => GetValues(metadata, ActiveModulesKey);
    public static IReadOnlyList<string> GetActiveApps(IReadOnlyDictionary<string, object?>? metadata) => GetValues(metadata, ActiveAppsKey);
    public static void SetActiveModules(IDictionary<string, object?> metadata, IEnumerable<string> modules) => metadata[ActiveModulesKey] = Normalize(modules);
    public static void SetActiveApps(IDictionary<string, object?> metadata, IEnumerable<string> apps) => metadata[ActiveAppsKey] = Normalize(apps);
    private static IReadOnlyList<string> GetValues(IReadOnlyDictionary<string, object?>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out object? raw) || raw is null) return [];
        try { return raw switch { JsonElement element => element.Deserialize<string[]>() ?? [], string[] values => values, IEnumerable<string> values => [.. values], System.Collections.IEnumerable values => [.. values.OfType<string>()], _ => [] }; }
        catch (JsonException) { return []; }
    }
    private static string[] Normalize(IEnumerable<string> values) => [.. values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)];
}
