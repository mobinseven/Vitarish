namespace Vitarish.Intelligence.Contracts;

/// <summary>
/// The stable, verified stages a streamed agent turn reports as <see cref="StreamChunkKind.Progress"/>.
/// Clients map each value to a localized label; the values never carry prompt, tool or answer content.
/// </summary>
public enum AgentTurnProgressStage
{
    ModelRoundStarted,
    ToolStarted,
    ToolCompleted,
    ValidatingResult,
    SavingConversation
}

/// <summary>
/// The typed payload of one streamed agent turn frame, carried as the <c>Delta</c> of an
/// <see cref="IntelligenceStreamChunk{T}"/>. <see cref="StreamChunkKind.Started"/> and
/// <see cref="StreamChunkKind.Canceled"/> carry no payload; <see cref="StreamChunkKind.Failed"/> carries the
/// chunk's redacted error.
/// </summary>
/// <param name="Stage">Set on <see cref="StreamChunkKind.Progress"/>.</param>
/// <param name="Ordinal">The model round or tool call count on progress, zero for app stages.</param>
/// <param name="MessageId">Set on <see cref="StreamChunkKind.Delta"/>: the stable id of the message being written.</param>
/// <param name="Text">Set on <see cref="StreamChunkKind.Delta"/>: displayable answer text only.</param>
/// <param name="Result">Set on <see cref="StreamChunkKind.Completed"/>: the authoritative buffered response.</param>
public sealed record AgentTurnPayload(
    AgentTurnProgressStage? Stage = null,
    int? Ordinal = null,
    string? MessageId = null,
    string? Text = null,
    RunAgentConversationResponse? Result = null);

/// <summary>What the registered agent conversation routes support, without trial requests.</summary>
public sealed record AgentConversationCapabilitiesResponse(IReadOnlyList<AgentConversationCapability> Agents);

/// <summary>The per-agent capability switch; <paramref name="StreamingEnabled"/> is off by default.</summary>
public sealed record AgentConversationCapability(string AgentKey, string SubjectKind, bool StreamingEnabled);
