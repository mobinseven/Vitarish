using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Intelligence.Application.Memory;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Intelligence.Infrastructure.Services.Memory;

/// <summary>
/// Keeps a person's memory in their profile under an Intelligence-owned key, without a table of its own
/// while the list stays small (ADR-0017).
/// </summary>
public sealed class ProfileUserMemoryStore(IUserProfileService profiles) : IUserMemoryStore
{
    public const string ProfileKey = "intelligence.memory";
    private const string IdProperty = "id";
    private const string TextProperty = "text";
    private const string SourceProperty = "source";
    private const string RememberedAtProperty = "rememberedAt";

    // Persian text stays as written instead of six-character escapes, so the memory fits the profile entry.
    private static readonly JsonSerializerOptions WriteOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async Task<UserMemorySnapshot> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> profile = await profiles.GetProfileAsync(userId, cancellationToken) ?? [];
        return profile.TryGetValue(ProfileKey, out string? stored) ? new(Read(stored), stored) : new([], null);
    }

    public async Task<bool> ReplaceAsync(string userId, IReadOnlyList<UserMemoryItem> items, string? expectedVersion, CancellationToken cancellationToken = default) =>
        await profiles.SetValueAsync(userId, ProfileKey, Write(items), new ProfileValueExpectation(expectedVersion), cancellationToken) == SetUserProfileValueOutcome.Saved;

    // Drops the oldest items until the rest fit one profile entry; no items removes the entry.
    private static string? Write(IReadOnlyList<UserMemoryItem> items)
    {
        for (int skip = 0; skip < items.Count; skip++)
        {
            JsonArray array = [];
            foreach (UserMemoryItem item in items.Skip(skip))
            {
                array.Add(new JsonObject
                {
                    [IdProperty] = item.Id,
                    [TextProperty] = item.Text,
                    [SourceProperty] = item.Source.ToString(),
                    [RememberedAtProperty] = item.RememberedAt.ToString("O", CultureInfo.InvariantCulture)
                });
            }

            string value = array.ToJsonString(WriteOptions);
            if (value.Length <= SetUserProfileValueCommand.MaxValueLength)
                return value;
        }

        return null;
    }

    // Entries that do not parse are skipped, so one bad entry never hides the rest.
    private static IReadOnlyList<UserMemoryItem> Read(string stored)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(stored);
        }
        catch (JsonException)
        {
            return [];
        }

        if (root is not JsonArray array)
            return [];
        List<UserMemoryItem> items = [];
        foreach (JsonNode? node in array)
        {
            if (node is JsonObject entry
                && entry[IdProperty]?.GetValue<string>() is { Length: > 0 } id
                && entry[TextProperty]?.GetValue<string>() is { Length: > 0 } text
                && Enum.TryParse(entry[SourceProperty]?.GetValue<string>(), out UserMemorySource source)
                && DateTimeOffset.TryParse(entry[RememberedAtProperty]?.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset rememberedAt))
            {
                items.Add(new UserMemoryItem(id, text, source, rememberedAt));
            }
        }

        return items;
    }
}
