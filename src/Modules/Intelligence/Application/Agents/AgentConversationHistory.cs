using System.Text;
using System.Text.Json.Nodes;

namespace Vitarish.Intelligence.Application.Agents;

/// <summary>
/// Rebuilds a stored model history so it can continue under the current agent configuration.
/// The stored history is a cache tied to the configuration that produced it; the conversation
/// itself survives configuration changes, so only the user and assistant messages are carried over.
/// </summary>
public static class AgentConversationHistory
{
    /// <summary>Histories larger than this are compacted before a run so the stored result stays within the store limit.</summary>
    public const int CompactionThresholdBytes = 512 * 1024;

    private static readonly HashSet<string> CarriedRoles = new(StringComparer.Ordinal) { "user", "assistant" };

    // Provider item identifiers and provider data link a message to items that are dropped here
    // (reasoning, tool calls); a provider rejects a message whose linked items are missing.
    private static readonly string[] ProviderLinkProperties = ["id", "providerData"];

    /// <summary>
    /// Keeps only user and assistant messages, without provider links, dropping the oldest
    /// messages until the result fits <paramref name="maximumBytes"/>. Returns <see langword="null"/>
    /// when nothing can be carried over.
    /// </summary>
    public static string? CarryOver(string? history, int maximumBytes = CompactionThresholdBytes)
    {
        if (string.IsNullOrWhiteSpace(history)) return null;
        JsonArray? items;
        try
        {
            items = JsonNode.Parse(history) as JsonArray;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        if (items is null) return null;

        List<(string Json, bool IsUser)> messages = [];
        foreach (JsonNode? item in items)
        {
            if (item is not JsonObject message || CarriedRole(message) is not { } role) continue;
            JsonObject carried = (JsonObject)message.DeepClone();
            foreach (string property in ProviderLinkProperties) carried.Remove(property);
            messages.Add((carried.ToJsonString(), role == "user"));
        }

        int bytes = 2 + messages.Sum(message => Encoding.UTF8.GetByteCount(message.Json) + 1);
        int first = 0;
        while (first < messages.Count && bytes > maximumBytes)
        {
            bytes -= Encoding.UTF8.GetByteCount(messages[first].Json) + 1;
            first++;
        }
        // A carried history starts with the person's turn, never with an orphaned assistant reply.
        while (first < messages.Count && !messages[first].IsUser)
            first++;
        return first >= messages.Count ? null : "[" + string.Join(',', messages.Skip(first).Select(message => message.Json)) + "]";
    }

    private static string? CarriedRole(JsonObject item)
    {
        if (item["type"] is JsonValue type && type.TryGetValue(out string? typeName) && typeName != "message")
            return null;
        return item["role"] is JsonValue role && role.TryGetValue(out string? roleName) && CarriedRoles.Contains(roleName)
            ? roleName
            : null;
    }
}
