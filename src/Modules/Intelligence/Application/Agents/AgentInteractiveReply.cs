using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vitarish.Intelligence.Application.Agents;

/// <summary>The reference and action keys a calling app lets its agent use; anything else is dropped.</summary>
public sealed record AgentInteractionVocabulary(
    IReadOnlyCollection<string> ReferenceKeys,
    IReadOnlyCollection<string> ActionKeys);

/// <summary>An agent reply that interfaces render as text, one question with options, references and one next step.</summary>
public sealed record AgentInteractiveReply(
    string Text,
    AgentInteractiveQuestion? Question,
    IReadOnlyList<string> References,
    string? NextAction);

public sealed record AgentInteractiveQuestion(
    string Prompt,
    IReadOnlyList<AgentInteractiveOption> Options,
    int MaxSelections);

public sealed record AgentInteractiveOption(string Key, string Label);

/// <summary>
/// Owns the interactive reply's result schema and the validation of model output against it. Model output is
/// untrusted: unknown references and actions are dropped, lengths are capped, a malformed question is removed,
/// and a reply without structured output falls back to its plain text.
/// </summary>
public static partial class AgentInteractiveReplyFormat
{
    public const string ResultName = "interactive_reply";
    public const string TextProperty = "text";
    public const string QuestionProperty = "question";
    public const string ReferencesProperty = "references";
    public const string NextActionProperty = "nextAction";
    public const string PromptProperty = "prompt";
    public const string OptionsProperty = "options";
    public const string MaxSelectionsProperty = "maxSelections";
    public const string OptionKeyProperty = "key";
    public const string OptionLabelProperty = "label";
    public const int MaxTextLength = 1_500;
    public const int MaxPromptLength = 200;
    public const int MinOptions = 2;
    public const int MaxOptions = 6;
    public const int MaxOptionLabelLength = 80;
    public const int MaxOptionKeyLength = 40;
    public const int MaxReferences = 4;

    /// <summary>Builds the strict result schema, with the app's keys as enums so the model is steered to them.</summary>
    public static AgentResultDefinition Definition(AgentInteractionVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        JsonObject option = StrictObject(new JsonObject
        {
            [OptionKeyProperty] = new JsonObject { ["type"] = "string" },
            [OptionLabelProperty] = new JsonObject { ["type"] = "string" }
        });
        JsonObject question = StrictObject(new JsonObject
        {
            [PromptProperty] = new JsonObject { ["type"] = "string" },
            [OptionsProperty] = new JsonObject { ["type"] = "array", ["items"] = option },
            [MaxSelectionsProperty] = new JsonObject { ["type"] = "integer" }
        });
        question["type"] = new JsonArray("object", "null");
        JsonObject root = StrictObject(new JsonObject
        {
            [TextProperty] = new JsonObject { ["type"] = "string" },
            [QuestionProperty] = question,
            [ReferencesProperty] = new JsonObject { ["type"] = "array", ["items"] = KeyedString(vocabulary.ReferenceKeys, nullable: false) },
            [NextActionProperty] = KeyedString(vocabulary.ActionKeys, nullable: true)
        });
        return new AgentResultDefinition(ResultName, root.ToJsonString());
    }

    /// <summary>Reads a run's output as an interactive reply, or returns null when it carries no usable text.</summary>
    public static AgentInteractiveReply? Read(AgentRunResult run, AgentInteractionVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(vocabulary);
        if (run.StructuredOutput is { ValueKind: JsonValueKind.Object } root
            && Text(root, TextProperty, MaxTextLength) is { } text)
        {
            return new AgentInteractiveReply(
                text,
                Question(root),
                References(root, vocabulary.ReferenceKeys),
                Key(root, NextActionProperty, vocabulary.ActionKeys));
        }

        string? plain = run.Output?.Trim();
        if (string.IsNullOrEmpty(plain) || plain.StartsWith('{'))
            return null;
        return new AgentInteractiveReply(Cap(plain, MaxTextLength), null, [], null);
    }

    private static AgentInteractiveQuestion? Question(JsonElement root)
    {
        if (!root.TryGetProperty(QuestionProperty, out JsonElement question) || question.ValueKind != JsonValueKind.Object)
            return null;
        if (Text(question, PromptProperty, MaxPromptLength) is not { } prompt
            || !question.TryGetProperty(OptionsProperty, out JsonElement options)
            || options.ValueKind != JsonValueKind.Array)
            return null;

        List<AgentInteractiveOption> parsed = [];
        HashSet<string> keys = new(StringComparer.Ordinal);
        foreach (JsonElement option in options.EnumerateArray())
        {
            if (option.ValueKind != JsonValueKind.Object
                || Text(option, OptionLabelProperty, MaxOptionLabelLength) is not { } label
                || Text(option, OptionKeyProperty, MaxOptionKeyLength) is not { } key
                || !OptionKeyPattern().IsMatch(key)
                || !keys.Add(key))
                continue;
            parsed.Add(new AgentInteractiveOption(key, label));
            if (parsed.Count == MaxOptions)
                break;
        }

        if (parsed.Count < MinOptions)
            return null;
        int maxSelections = question.TryGetProperty(MaxSelectionsProperty, out JsonElement max) && max.TryGetInt32(out int value)
            ? Math.Clamp(value, 1, parsed.Count)
            : 1;
        return new AgentInteractiveQuestion(prompt, parsed, maxSelections);
    }

    private static IReadOnlyList<string> References(JsonElement root, IReadOnlyCollection<string> allowed)
    {
        if (!root.TryGetProperty(ReferencesProperty, out JsonElement references) || references.ValueKind != JsonValueKind.Array)
            return [];
        List<string> kept = [];
        foreach (JsonElement reference in references.EnumerateArray())
        {
            if (reference.ValueKind == JsonValueKind.String
                && reference.GetString() is { } key
                && allowed.Contains(key)
                && !kept.Contains(key, StringComparer.Ordinal))
                kept.Add(key);
            if (kept.Count == MaxReferences)
                break;
        }

        return kept;
    }

    private static string? Key(JsonElement root, string property, IReadOnlyCollection<string> allowed) =>
        root.TryGetProperty(property, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } key
        && allowed.Contains(key)
            ? key
            : null;

    private static string? Text(JsonElement element, string property, int maxLength) =>
        element.TryGetProperty(property, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString()?.Trim() is { Length: > 0 } text
            ? Cap(text, maxLength)
            : null;

    private static string Cap(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";

    private static JsonObject StrictObject(JsonObject properties)
    {
        JsonArray required = [];
        foreach (KeyValuePair<string, JsonNode?> property in properties)
            required.Add(property.Key);
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    private static JsonObject KeyedString(IReadOnlyCollection<string> keys, bool nullable)
    {
        JsonObject schema = new() { ["type"] = nullable ? new JsonArray("string", "null") : JsonValue.Create("string") };
        if (keys.Count == 0)
            return schema;
        JsonArray values = [];
        foreach (string key in keys)
            values.Add(key);
        if (nullable)
            values.Add(null);
        schema["enum"] = values;
        return schema;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex OptionKeyPattern();
}
