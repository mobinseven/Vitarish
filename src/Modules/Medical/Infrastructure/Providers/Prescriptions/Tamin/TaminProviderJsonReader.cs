using System.Text.Json;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal static class TaminProviderJsonReader
{
    public static TaminReferencePageResult ToReferencePage(string codeSystem, JsonElement root, int? fallbackPage, int? fallbackPageSize)
    {
        JsonElement envelope = FindEnvelope(root);
        IReadOnlyList<TaminReferenceItem> items = EnumerateItems(root)
            .Select(item => ToReferenceItem(codeSystem, item))
            .Where(item => !string.IsNullOrWhiteSpace(item.Code) || !string.IsNullOrWhiteSpace(item.DisplayName))
            .ToList();

        int? page = GetInt(envelope, "page") ?? fallbackPage;
        int? pageSize = GetInt(envelope, "page_size") ?? GetInt(envelope, "pageSize") ?? fallbackPageSize;
        int? total = GetInt(envelope, "total") ?? GetInt(envelope, "total_count") ?? GetInt(envelope, "totalCount");
        bool? hasNext = GetBool(envelope, "has_next") ?? GetBool(envelope, "hasNext") ?? InferHasNext(envelope, page, pageSize, total, items.Count);
        string version = GetString(envelope, "version") ?? GetString(envelope, "data_version") ?? GetString(envelope, "dataVersion") ?? "unknown";

        return new TaminReferencePageResult(codeSystem, page, pageSize, total, hasNext, version, items, root.GetRawText());
    }

    private static TaminReferenceItem ToReferenceItem(string codeSystem, JsonElement item) => new(
        GetFirstString(item, ["drug_code", "drugCode", "service_code", "serviceCode", "srvCode", "code", "id"]) ?? string.Empty,
        GetFirstString(item, ["drug_name", "drugName", "service_name", "serviceName", "srvName", "name", "title"]) ?? string.Empty,
        GetFirstString(item, ["category", "service_group", "serviceGroup", "service_type", "serviceType", "group", "type"]),
        GetBool(item, "is_active") ?? GetBool(item, "active") ?? !string.Equals(GetString(item, "status"), "inactive", StringComparison.OrdinalIgnoreCase),
        codeSystem,
        item.GetRawText());

    private static IEnumerable<JsonElement> EnumerateItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray();
        }

        foreach (string path in new[] { "items", "data", "data.items", "result", "result.items", "list", "data.list" })
        {
            if (TryGetPath(root, path, out JsonElement found) && found.ValueKind == JsonValueKind.Array)
            {
                return found.EnumerateArray();
            }
        }

        return [];
    }

    private static bool? InferHasNext(JsonElement root, int? page, int? pageSize, int? total, int count)
    {
        int? nextPage = GetInt(root, "next_page") ?? GetInt(root, "nextPage");
        if (nextPage.HasValue) return true;
        if (page.HasValue && pageSize.HasValue && total.HasValue) return page.Value * pageSize.Value < total.Value;
        if (pageSize.HasValue && count < pageSize.Value) return false;
        return null;
    }


    private static JsonElement FindEnvelope(JsonElement root)
    {
        foreach (string path in new[] { "data", "result" })
        {
            if (TryGetPath(root, path, out JsonElement found) && found.ValueKind == JsonValueKind.Object)
            {
                return found;
            }
        }

        return root;
    }

    private static bool TryGetPath(JsonElement root, string path, out JsonElement value)
    {
        value = root;
        foreach (string segment in path.Split('.'))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value)) return false;
        }
        return true;
    }

    private static string? GetFirstString(JsonElement element, IReadOnlyList<string> names) => names.Select(name => GetString(element, name)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static string? GetString(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement prop) ? prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString() : null;
    private static int? GetInt(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement prop) && prop.TryGetInt32(out int value) ? value : null;
    private static bool? GetBool(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement prop) && (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False) ? prop.GetBoolean() : null;
}
