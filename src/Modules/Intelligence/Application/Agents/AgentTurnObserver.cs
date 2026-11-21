namespace Vitarish.Intelligence.Application.Agents;

/// <summary>A trusted point in a conversation turn that Main can verify and report while the turn runs.</summary>
public enum AgentTurnStage
{
    /// <summary>The runtime started a model round.</summary>
    ModelRoundStarted,
    /// <summary>The runtime started a guarded tool call.</summary>
    ToolStarted,
    /// <summary>A guarded tool call finished.</summary>
    ToolCompleted,
    /// <summary>The app is validating the final runtime result before it is committed.</summary>
    ValidatingResult,
    /// <summary>The conversation history and prepared work are being persisted.</summary>
    SavingConversation
}

/// <summary>One stage report. <paramref name="Ordinal"/> counts model rounds or tool calls and is zero for app stages.</summary>
public sealed record AgentTurnStageEvent(AgentTurnStage Stage, int Ordinal = 0);

/// <summary>
/// Declares in advance, before the model runs, that the string at <paramref name="FieldPath"/> (object keys
/// from the root) of the structured result is the user-visible answer and may be streamed as provisional text
/// under <paramref name="MessageId"/>. The runtime honors it only for a run without tools.
/// </summary>
public sealed record AgentAnswerStream(string MessageId, IReadOnlyList<string> FieldPath);

/// <summary>Receives the stages and answer text a runtime reports while it executes one turn.</summary>
public interface IAgentRunProgress
{
    ValueTask StageAsync(AgentTurnStageEvent stage, CancellationToken cancellationToken);

    /// <summary>
    /// Provisional answer text of the declared <see cref="AgentAnswerStream"/>; the terminal result stays
    /// authoritative. Only reported when the run declared an answer stream.
    /// </summary>
    ValueTask AnswerDeltaAsync(string messageId, string text, CancellationToken cancellationToken);
}

/// <summary>
/// Observes one conversation turn run in streamed mode. The same workflow serves buffered turns, which
/// pass no observer. An observer reports only; it never decides the outcome of the turn.
/// </summary>
public interface IAgentTurnObserver : IAgentRunProgress
{
    /// <summary>
    /// True once any provisional answer text was shown for the turn. History recovery must then fail the
    /// turn instead of silently replacing the attempt that produced that text.
    /// </summary>
    bool AnswerTextEmitted { get; }

    /// <summary>
    /// The turn passed lease acquisition and will execute. <paramref name="turnDeadline"/> is the
    /// server-side bound for the remaining turn.
    /// </summary>
    ValueTask AcceptedAsync(TimeSpan turnDeadline, CancellationToken cancellationToken);
}
