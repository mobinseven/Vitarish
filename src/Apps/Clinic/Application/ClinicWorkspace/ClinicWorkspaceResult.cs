namespace Vitarish.Clinic.Application.ClinicWorkspace;

public sealed record ClinicWorkspaceResult(
    string AgentRunId,
    string ConversationId,
    IReadOnlyList<ClinicWorkspaceMessage> Messages,
    IReadOnlyList<ClinicWorkspaceAction> Actions,
    IReadOnlyList<Operations.ClinicWorkspaceSearchCandidate>? Candidates = null,
    bool HasMoreCandidates = false)
{
    public ClinicWorkspaceRunOutcome Outcome { get; init; } = ClinicWorkspaceRunOutcome.Succeeded;
    public IReadOnlyList<ClinicWorkspacePreparedAction> PreparedActions { get; init; } = [];

    public static ClinicWorkspaceResult Failure(string agentRunId, string conversationId, string message) =>
        new(agentRunId, conversationId, [new ClinicWorkspaceMessage(ClinicWorkspaceMessageKind.Failure, message)], [])
        {
            Outcome = ClinicWorkspaceRunOutcome.Failed
        };
}

public enum ClinicWorkspaceRunOutcome
{
    Succeeded,
    NotFound,
    Busy,
    ProfileChanged,
    InvalidHistory,
    Failed
}
