using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

/// <summary>
/// One validated line of the runtime's internal NDJSON stream. A stage frame carries a trusted stage;
/// the terminal result frame carries no stage and is deserialized separately as the buffered response.
/// </summary>
internal readonly record struct AgentRuntimeStreamFrame(AgentTurnStageEvent? Stage)
{
    internal const string KindProperty = "kind";
    internal const string StageKind = "stage";
    internal const string ResultKind = "result";
    internal const string StageProperty = "stage";
    internal const string OrdinalProperty = "ordinal";

    /// <summary>Validates a frame; anything unknown, oversized or malformed throws instead of being skipped.</summary>
    internal static AgentRuntimeStreamFrame Parse(ReadOnlySpan<byte> line)
    {
        Utf8JsonReader probe = new(line);
        using JsonDocument document = JsonDocument.ParseValue(ref probe);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(KindProperty, out JsonElement kind) || kind.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("The runtime stream frame has no kind.");

        if (kind.ValueEquals(ResultKind))
            return default;
        if (!kind.ValueEquals(StageKind))
            throw new InvalidDataException("The runtime stream frame kind is unknown.");

        if (line.Length > AgentRuntimeHttpRunner.MaxStageFrameBytes)
            throw new InvalidDataException("The runtime stage frame exceeds the size limit.");
        if (!root.TryGetProperty(StageProperty, out JsonElement stage) || stage.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty(OrdinalProperty, out JsonElement ordinal) || !ordinal.TryGetInt32(out int number) || number < 0)
            throw new InvalidDataException("The runtime stage frame is malformed.");

        AgentTurnStage? mapped =
            stage.ValueEquals(nameof(AgentTurnStage.ModelRoundStarted)) ? AgentTurnStage.ModelRoundStarted :
            stage.ValueEquals(nameof(AgentTurnStage.ToolStarted)) ? AgentTurnStage.ToolStarted :
            stage.ValueEquals(nameof(AgentTurnStage.ToolCompleted)) ? AgentTurnStage.ToolCompleted :
            null;
        return mapped is { } known
            ? new AgentRuntimeStreamFrame(new AgentTurnStageEvent(known, number))
            : throw new InvalidDataException("The runtime stage is unknown.");
    }
}
