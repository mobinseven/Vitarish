using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

/// <summary>
/// One validated line of the runtime's internal NDJSON stream. A stage frame carries a trusted stage, a
/// delta frame carries provisional answer text of the declared answer stream; the terminal result frame
/// carries neither and is deserialized separately as the buffered response.
/// </summary>
internal readonly record struct AgentRuntimeStreamFrame(AgentTurnStageEvent? Stage, string? DeltaText = null)
{
    internal const string KindProperty = "kind";
    internal const string StageKind = "stage";
    internal const string DeltaKind = "delta";
    internal const string ResultKind = "result";
    internal const string StageProperty = "stage";
    internal const string OrdinalProperty = "ordinal";
    internal const string MessageIdProperty = "messageId";
    internal const string TextProperty = "text";

    /// <summary>The largest delta text the runtime emits, in UTF-16 code units.</summary>
    internal const int MaxDeltaChars = 2048;

    /// <summary>The largest answer text accepted for one run, in UTF-16 code units.</summary>
    internal const int MaxAnswerChars = 100_000;

    /// <summary>The largest accepted delta frame: escaped text of <see cref="MaxDeltaChars"/> plus the envelope.</summary>
    internal const int MaxDeltaFrameBytes = 16 * 1024;

    /// <summary>
    /// Validates a frame; anything unknown, oversized or malformed throws instead of being skipped. A delta
    /// is only valid for the <paramref name="answerStream"/> the run declared.
    /// </summary>
    internal static AgentRuntimeStreamFrame Parse(ReadOnlySpan<byte> line, AgentAnswerStream? answerStream = null)
    {
        Utf8JsonReader probe = new(line);
        using JsonDocument document = JsonDocument.ParseValue(ref probe);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(KindProperty, out JsonElement kind) || kind.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("The runtime stream frame has no kind.");

        if (kind.ValueEquals(ResultKind))
            return default;
        if (kind.ValueEquals(DeltaKind))
            return ParseDelta(line.Length, root, answerStream);
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

    private static AgentRuntimeStreamFrame ParseDelta(int lineLength, JsonElement root, AgentAnswerStream? answerStream)
    {
        if (answerStream is null)
            throw new InvalidDataException("The runtime streamed answer text that the run did not declare.");
        if (lineLength > MaxDeltaFrameBytes)
            throw new InvalidDataException("The runtime delta frame exceeds the size limit.");
        if (!root.TryGetProperty(MessageIdProperty, out JsonElement messageId) || messageId.ValueKind != JsonValueKind.String ||
            !messageId.ValueEquals(answerStream.MessageId))
            throw new InvalidDataException("The runtime delta frame names another message.");
        if (!root.TryGetProperty(TextProperty, out JsonElement text) || text.ValueKind != JsonValueKind.String ||
            text.GetString() is not { Length: > 0 and <= MaxDeltaChars } value)
            throw new InvalidDataException("The runtime delta frame text is malformed.");
        return new AgentRuntimeStreamFrame(null, value);
    }
}
