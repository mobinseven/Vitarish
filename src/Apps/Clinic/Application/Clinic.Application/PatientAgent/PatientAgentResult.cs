namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentResult(
    string AgentRunId,
    string ConversationId,
    string? PreviousResponseId,
    IReadOnlyList<PatientAgentMessage> Messages,
    IReadOnlyList<PatientAgentAction> Actions)
{
    public static PatientAgentResult Failure(string agentRunId, string conversationId, string message) =>
        new(agentRunId, conversationId, null, [new PatientAgentMessage(PatientAgentMessageKind.Failure, message)], []);
}
