using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Application.GuidedSurfaces;

/// <summary>How the web formats one presentable field value.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PresentableFieldType>))]
public enum PresentableFieldType
{
    Text,
    Date,
    DateTime,
    Number,
    Status
}

/// <summary>One value a surface may show. Values are already masked where the owning screen masks them.</summary>
public sealed record PresentableField(PresentableFieldType Type, JsonElement Value)
{
    public static PresentableField Text(string? value) => new(PresentableFieldType.Text, JsonSerializer.SerializeToElement(value, PresentableJsonContext.Default.String));
    public static PresentableField Status(string? value) => new(PresentableFieldType.Status, JsonSerializer.SerializeToElement(value, PresentableJsonContext.Default.String));
    public static PresentableField Date(string? isoDate) => new(PresentableFieldType.Date, JsonSerializer.SerializeToElement(isoDate, PresentableJsonContext.Default.String));
    public static PresentableField DateTime(string? isoDateTime) => new(PresentableFieldType.DateTime, JsonSerializer.SerializeToElement(isoDateTime, PresentableJsonContext.Default.String));
    public static PresentableField Number(double? value) => new(PresentableFieldType.Number, value is null ? default : JsonSerializer.SerializeToElement(value.Value, PresentableJsonContext.Default.Double));
}

/// <summary>
/// One record of a presentable result. <paramref name="Kind"/> is the app's record kind key (it chooses the
/// localized field labels and the screen a record opens); <paramref name="Reference"/> is what the web uses to open
/// it (an ID, or an opaque token where the screen requires one), never more than the owning screen would hold.
/// <paramref name="Title"/> is typed like a field, so a date or status title is formatted for the viewer.
/// </summary>
public sealed record PresentableRecord(
    string Kind,
    string Reference,
    PresentableField Title,
    IReadOnlyDictionary<string, PresentableField> Fields);

/// <summary>The part of one successful read tool result that a presented surface may show (ADR-0022 decision 8).</summary>
public sealed record PresentableResult(
    string Kind,
    bool IsList,
    IReadOnlyList<PresentableRecord> Records,
    bool HasMore = false)
{
    [JsonIgnore]
    public IReadOnlyList<string> FieldNames => Records.SelectMany(record => record.Fields.Keys).Distinct(StringComparer.Ordinal).ToArray();
}

/// <summary>
/// Declares that one tool of the named agents returns a presentable result. Registered explicitly by the app that
/// owns the agent; the projector receives the tool's successful output and returns null when nothing can be shown.
/// </summary>
public sealed record AgentToolPresentation(
    IReadOnlyCollection<string> AgentKeys,
    string ToolName,
    Func<JsonElement, PresentableResult?> Project);

public sealed class AgentToolPresentationCatalog
{
    private readonly Dictionary<(string AgentKey, string ToolName), AgentToolPresentation> _presentations = new();

    public AgentToolPresentationCatalog(IEnumerable<AgentToolPresentation> presentations)
    {
        ArgumentNullException.ThrowIfNull(presentations);
        foreach (AgentToolPresentation presentation in presentations)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(presentation.ToolName);
            foreach (string agentKey in presentation.AgentKeys)
            {
                if (!_presentations.TryAdd((agentKey, presentation.ToolName), presentation))
                    throw new InvalidOperationException($"Tool '{presentation.ToolName}' of agent '{agentKey}' declares more than one presentation.");
            }
        }
    }

    public AgentToolPresentation? Find(string agentKey, string toolName) =>
        _presentations.GetValueOrDefault((agentKey, toolName));

    public bool HasPresentations(string agentKey) => _presentations.Keys.Any(key => key.AgentKey == agentKey);
}

/// <summary>Keeps the presentable results of one run, by handle, for minutes.</summary>
public interface IPresentableResultStore
{
    /// <summary>Saves the result and returns its new handle, unique within the run.</summary>
    Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken);

    Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken);
}

/// <summary>
/// Attaches a handle to a successful read result that its agent declares presentable. The handle is added to the
/// tool output the model reads, as a <c>presentation</c> property; the read itself never fails because of this.
/// </summary>
public sealed partial class PresentableResultPublisher(
    AgentToolPresentationCatalog catalog,
    IPresentableResultStore store,
    ILogger<PresentableResultPublisher> logger)
{
    public const string OutputProperty = "presentation";

    public Task<AgentToolResult> PublishAsync(
        AgentRuntimeExecutionEnvelope envelope,
        AgentToolResult result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return PublishAsync(envelope.AgentKey, envelope.ToolName, envelope.ExecutionId, result, cancellationToken);
    }

    public async Task<AgentToolResult> PublishAsync(
        string agentKey,
        string toolName,
        string executionId,
        AgentToolResult result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsError) return result;
        AgentToolPresentation? presentation = catalog.Find(agentKey, toolName);
        if (presentation is null) return result;

        try
        {
            if (JsonNode.Parse(result.Output) is not JsonObject output) return result;
            using JsonDocument document = JsonDocument.Parse(result.Output);
            PresentableResult? presentable = presentation.Project(document.RootElement);
            if (presentable is null || presentable.Records.Count == 0) return result;

            string handle = await store.SaveAsync(executionId, presentable, cancellationToken);
            output[OutputProperty] = new JsonObject
            {
                ["handle"] = handle,
                ["kind"] = presentable.Kind,
                ["isList"] = presentable.IsList,
                ["records"] = presentable.Records.Count,
                ["fields"] = new JsonArray(presentable.FieldNames.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray())
            };
            return result with { Output = output.ToJsonString() };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A read must not fail because it cannot be shown; the model then answers in text.
            LogPublishFailed(logger, toolName, exception.GetType().Name, executionId);
            return result;
        }
    }

    [LoggerMessage(EventId = 73620, Level = LogLevel.Warning, Message = "A presentable result of {ToolName} could not be published ({ErrorType}); execution {ExecutionId}.")]
    private static partial void LogPublishFailed(ILogger logger, string toolName, string errorType, string executionId);
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(PresentableResult))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
public sealed partial class PresentableJsonContext : JsonSerializerContext;
