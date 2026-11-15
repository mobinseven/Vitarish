using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Application.GuidedSurfaces;

/// <summary>
/// The Vitarish catalog of the UI protocol (ADR-0022 decision 8). Intelligence owns the one catalog document; the
/// web renders the same file and the agent runtime validates surfaces against it.
/// </summary>
public static class AssistantSurfaceCatalog
{
    public const string ResourceName = "Vitarish.Intelligence.AssistantSurfaceCatalog.json";

    /// <summary>The protocol the stored and transported surfaces use.</summary>
    public const string Protocol = "a2ui";

    private static readonly Lazy<string> Document = new(() =>
    {
        using Stream stream = typeof(AssistantSurfaceCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The assistant surface catalog is not embedded.");
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    });

    private static readonly Lazy<(string Id, string Version, string Instructions, string Components)> Parsed = new(() =>
    {
        using JsonDocument document = JsonDocument.Parse(Document.Value);
        JsonElement root = document.RootElement;
        StringBuilder components = new();
        foreach (JsonProperty component in root.GetProperty("components").EnumerateObject())
        {
            components.Append("- ").Append(component.Name).Append(": ")
                .Append(component.Value.GetProperty("description").GetString()).Append(" Properties: ");
            components.Append(string.Join(", ", component.Value.GetProperty("properties").EnumerateObject()
                .Where(property => property.Name != "component")
                .Select(property => property.Name)));
            components.Append('\n');
        }
        return (root.GetProperty("catalogId").GetString()!, root.GetProperty("protocolVersion").GetString()!,
            root.GetProperty("instructions").GetString()!, components.ToString());
    });

    public static string Json => Document.Value;
    public static string Id => Parsed.Value.Id;
    public static string ProtocolVersion => Parsed.Value.Version;
    public static string Instructions => Parsed.Value.Instructions;
    public static string ComponentSummary => Parsed.Value.Components;

    /// <summary>
    /// The present tool's parameters as a strict function schema derived from the catalog: one exact shape per
    /// component, so the model fills fields instead of writing the surface as free JSON text. Every property is
    /// required, as strict schemas demand; length and count limits move into descriptions and the catalog
    /// validation in the agent runtime still enforces them.
    /// </summary>
    public static string ToolParameters => ParametersSchema.Value;

    private static readonly Lazy<string> ParametersSchema = new(() =>
    {
        using JsonDocument document = JsonDocument.Parse(Document.Value);
        JsonArray shapes = [];
        foreach (JsonProperty component in document.RootElement.GetProperty("components").EnumerateObject())
        {
            JsonObject properties = new()
            {
                ["id"] = new JsonObject { ["type"] = "string", ["description"] = "Unique within the surface; the top component's id is \"root\"." },
                ["component"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray(component.Name) }
            };
            foreach (JsonProperty property in component.Value.GetProperty("properties").EnumerateObject())
            {
                if (property.Name != "component")
                    properties[property.Name] = StrictProperty(property.Value);
            }
            shapes.Add(new JsonObject
            {
                ["type"] = "object",
                ["description"] = $"{component.Name}: {component.Value.GetProperty("description").GetString()}",
                ["properties"] = properties,
                ["required"] = new JsonArray([.. properties.Select(property => (JsonNode)property.Key)]),
                ["additionalProperties"] = false
            });
        }
        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["surface"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["components"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["description"] = "The components; exactly one has id \"root\". Use a Column as root to show more than one.",
                            ["items"] = new JsonObject { ["anyOf"] = shapes }
                        }
                    },
                    ["required"] = new JsonArray("components"),
                    ["additionalProperties"] = false
                }
            },
            ["required"] = new JsonArray("surface"),
            ["additionalProperties"] = false
        };
        return schema.ToJsonString();
    });

    private static JsonObject StrictProperty(JsonElement property)
    {
        string description = property.TryGetProperty("description", out JsonElement text) ? text.GetString() ?? string.Empty : string.Empty;
        if (property.TryGetProperty("$ref", out JsonElement reference))
        {
            string name = reference.GetString()!;
            if (name.EndsWith("/DataBinding", StringComparison.Ordinal))
                return new JsonObject
                {
                    ["type"] = "object",
                    ["description"] = description,
                    ["properties"] = new JsonObject { ["path"] = new JsonObject { ["type"] = "string", ["description"] = "\"/\" followed by the handle from the read result's presentation." } },
                    ["required"] = new JsonArray("path"),
                    ["additionalProperties"] = false
                };
            if (name.EndsWith("/ChildList", StringComparison.Ordinal))
                return new JsonObject { ["type"] = "array", ["description"] = description, ["items"] = new JsonObject { ["type"] = "string" } };
            throw new InvalidOperationException($"The assistant surface catalog references an unsupported type {name}.");
        }
        string type = property.GetProperty("type").GetString()!;
        List<string> limits = [];
        if (property.TryGetProperty("maxItems", out JsonElement maxItems)) limits.Add($"At most {maxItems.GetInt32()} items.");
        if (property.TryGetProperty("maxLength", out JsonElement maxLength)) limits.Add($"At most {maxLength.GetInt32()} characters.");
        if (property.TryGetProperty("minimum", out JsonElement minimum)) limits.Add($"At least {minimum.GetInt32()}.");
        JsonObject strict = new() { ["type"] = type, ["description"] = string.Join(' ', [description, .. limits]).Trim() };
        if (type == "array")
        {
            strict["items"] = new JsonObject { ["type"] = property.GetProperty("items").GetProperty("type").GetString() };
            // Strict schemas support item counts, so the model is held to the limit, not only told it.
            if (property.TryGetProperty("maxItems", out JsonElement count)) strict["maxItems"] = count.GetInt32();
        }
        return strict;
    }
}

/// <summary>One surface the model presented, with the data the server filled for it.</summary>
public sealed record PresentedSurface(
    string SurfaceId,
    string Protocol,
    string ProtocolVersion,
    string CatalogId,
    JsonElement Components,
    JsonElement DataModel);

/// <summary>Reads the bindings of a surface and checks each against the handles of the run.</summary>
public static class PresentedSurfaceBindings
{
    private const int MaximumComponents = 12;
    private static readonly string[] DataProperties = ["result"];

    /// <summary>Returns the components of a <c>present_surface</c> argument, or a reason it cannot be shown.</summary>
    public static (JsonArray? Components, string? Problem) ReadComponents(string argumentsJson)
    {
        JsonNode? arguments;
        try { arguments = JsonNode.Parse(argumentsJson); }
        catch (JsonException) { return (null, "The arguments are not JSON."); }
        JsonNode? surface = arguments?["surface"];
        if (surface is JsonValue text && text.TryGetValue(out string? encoded))
        {
            try { surface = JsonNode.Parse(encoded); }
            catch (JsonException) { return (null, "The surface is not JSON."); }
        }
        if (surface?["components"] is not JsonArray components || components.Count == 0)
            return (null, "The surface has no components.");
        if (components.Count > MaximumComponents)
            return (null, $"A surface has at most {MaximumComponents} components.");
        return (components, null);
    }

    /// <summary>The handles each data property binds, with the field names the component names.</summary>
    public static IReadOnlyList<(string Handle, IReadOnlyList<string> Fields)> Bindings(JsonArray components, out string? problem)
    {
        problem = null;
        List<(string, IReadOnlyList<string>)> bindings = [];
        foreach (JsonNode? node in components)
        {
            if (node is not JsonObject component) { problem = "Each component must be an object."; return []; }
            foreach (string property in DataProperties)
            {
                if (!component.TryGetPropertyValue(property, out JsonNode? value)) continue;
                string? path = value is JsonObject binding && (binding["path"] ?? binding["@path"]) is JsonValue pointer && pointer.TryGetValue(out string? text) ? text : null;
                if (path is null || path.Length < 2 || path[0] != '/' || path.IndexOf('/', 1) >= 0)
                {
                    problem = "A result must be bound as {\"path\": \"/<handle>\"} to a presentation handle.";
                    return [];
                }
                List<string> fields = [];
                if (component["fields"] is JsonArray names)
                    fields.AddRange(names.Select(name => name is JsonValue text && text.TryGetValue(out string? field) ? field : string.Empty));
                if (component["field"] is JsonValue single && single.TryGetValue(out string? field))
                    fields.Add(field);
                bindings.Add((path[1..], fields));
            }
        }
        return bindings;
    }
}

/// <summary>
/// The shared tool through which a model shows records it read in this run (ADR-0022 decision 8). The agent
/// runtime validates the surface against the catalog first; this tool checks that every binding names a handle of
/// this run and only fields that result may show.
/// </summary>
public sealed class PresentSurfaceTool(IPresentableResultStore store) : IIntelligenceAgentTool
{
    public const string Name = "present_surface";

    /// <summary>Set in the execution metadata when the agent runtime validated the surface against the catalog.</summary>
    public const string ValidatedMetadataKey = "surfaceValidated";

    public AgentToolDefinition Definition { get; } = new(
        Name,
        $"Show the person records you read in this turn as cards, lists, numbers or a chart, in addition to your reply. " +
        $"Only results whose output has a \"{PresentableResultPublisher.OutputProperty}\" property can be shown. " +
        $"{AssistantSurfaceCatalog.Instructions}\nComponents (catalog {AssistantSurfaceCatalog.Id}):\n{AssistantSurfaceCatalog.ComponentSummary}" +
        "Example for a list result with handle r1-ab12: {\"surface\":{\"components\":[{\"id\":\"root\",\"component\":\"RecordList\",\"result\":{\"path\":\"/r1-ab12\"},\"fields\":[\"status\",\"date\"]}]}}.",
        AssistantSurfaceCatalog.ToolParameters,
        AssistantSurfaceCatalog.Json);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(context);
        if (context.Metadata?.GetValueOrDefault(ValidatedMetadataKey) != "true")
            return Error(call, "The surface was not validated against the catalog.");

        (JsonArray? components, string? problem) = PresentedSurfaceBindings.ReadComponents(call.ArgumentsJson);
        if (components is null) return Error(call, problem!);
        IReadOnlyList<(string Handle, IReadOnlyList<string> Fields)> bindings = PresentedSurfaceBindings.Bindings(components, out problem);
        if (problem is not null) return Error(call, problem);
        if (bindings.Count == 0) return Error(call, "A surface must show at least one result.");

        foreach ((string handle, IReadOnlyList<string> fields) in bindings)
        {
            PresentableResult? result = await store.GetAsync(context.ExecutionId, handle, cancellationToken);
            if (result is null) return Error(call, $"'{handle}' is not a presentation handle of this turn.");
            string? unknown = fields.FirstOrDefault(field => !result.FieldNames.Contains(field, StringComparer.Ordinal));
            if (unknown is not null) return Error(call, $"'{unknown}' is not a field of '{handle}'. Its fields are: {string.Join(", ", result.FieldNames)}.");
        }
        return new AgentToolResult(call.Id, """{"status":"shown"}""");
    }

    private static AgentToolResult Error(AgentToolCall call, string message) =>
        new(call.Id, new JsonObject { ["error"] = message }.ToJsonString(), IsError: true);
}

/// <summary>Builds the surfaces a run presented, with data filled from the run's handles.</summary>
public sealed class PresentedSurfaceCollector(IPresentableResultStore store)
{
    public async Task<IReadOnlyList<PresentedSurface>> CollectAsync(
        string executionId,
        IReadOnlyList<AgentToolExecution> executions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executions);
        List<PresentedSurface> surfaces = [];
        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError || execution.ToolName != PresentSurfaceTool.Name || execution.ArgumentsJson is null) continue;
            (JsonArray? components, _) = PresentedSurfaceBindings.ReadComponents(execution.ArgumentsJson);
            if (components is null) continue;
            IReadOnlyList<(string Handle, IReadOnlyList<string> Fields)> bindings = PresentedSurfaceBindings.Bindings(components, out string? problem);
            if (problem is not null) continue;

            JsonObject data = [];
            bool complete = true;
            foreach (string handle in bindings.Select(binding => binding.Handle).Distinct(StringComparer.Ordinal))
            {
                PresentableResult? result = await store.GetAsync(executionId, handle, cancellationToken);
                if (result is null) { complete = false; break; }
                data[handle] = JsonSerializer.SerializeToNode(result, PresentableJsonContext.Default.PresentableResult);
            }
            // A surface whose data expired is dropped; the reply text still answers.
            if (!complete) continue;
            surfaces.Add(new(
                execution.ToolCallId,
                AssistantSurfaceCatalog.Protocol,
                AssistantSurfaceCatalog.ProtocolVersion,
                AssistantSurfaceCatalog.Id,
                JsonSerializer.SerializeToElement(components, PresentableJsonContext.Default.JsonArray),
                JsonSerializer.SerializeToElement(data, PresentableJsonContext.Default.JsonObject)));
        }
        return surfaces;
    }
}
