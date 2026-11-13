namespace Vitarish.Clinic.Application.Assistant;

public sealed record AssistantResult(
    string AgentRunId,
    string ConversationId,
    IReadOnlyList<AssistantMessage> Messages,
    IReadOnlyList<AssistantAction> Actions,
    IReadOnlyList<Operations.AssistantSearchCandidate>? Candidates = null,
    bool HasMoreCandidates = false)
{
    public AssistantRunOutcome Outcome { get; init; } = AssistantRunOutcome.Succeeded;
    public IReadOnlyList<AssistantPreparedAction> PreparedActions { get; init; } = [];

    /// <summary>The model's validated question for the next step, if it asked one.</summary>
    public Vitarish.Intelligence.Application.Agents.AgentInteractiveQuestion? Question { get; init; }

    /// <summary>The setup goal whose screen the model suggests opening next, if any.</summary>
    public string? NextAction { get; init; }

    public static AssistantResult Failure(string agentRunId, string conversationId, string message) =>
        new(agentRunId, conversationId, [new AssistantMessage(AssistantMessageKind.Failure, message)], [])
        {
            Outcome = AssistantRunOutcome.Failed
        };
}

public enum AssistantRunOutcome
{
    Succeeded,
    NotFound,
    Busy,
    ProfileChanged,
    InvalidHistory,
    Failed
}
