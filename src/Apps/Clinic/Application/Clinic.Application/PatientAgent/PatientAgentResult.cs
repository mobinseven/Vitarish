namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentResult(
    string AgentRunId,
    string ConversationId,
    IReadOnlyList<PatientAgentMessage> Messages,
    IReadOnlyList<PatientAgentAction> Actions)
{
    public PatientAgentRunOutcome Outcome { get; init; } = PatientAgentRunOutcome.Succeeded;

    public static PatientAgentResult Failure(string agentRunId, string conversationId, string message) =>
        new(agentRunId, conversationId, [new PatientAgentMessage(PatientAgentMessageKind.Failure, message)], [])
        {
            Outcome = PatientAgentRunOutcome.Failed
        };
}

public enum PatientAgentRunOutcome
{
    Succeeded,
    NotFound,
    Busy,
    Failed
}
