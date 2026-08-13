namespace Vitarish.Clinic.Application.Assistant.Operations;

public static class AssistantOperationsScope
{
    public const string TenantOwnerRole = "TenantOwner";
    public const string PlatformAdminRole = "PlatformAdmin";

    public static bool IsAuthorizedRole(string? role) =>
        string.Equals(role, TenantOwnerRole, StringComparison.Ordinal) ||
        string.Equals(role, PlatformAdminRole, StringComparison.Ordinal);
}

public sealed record AssistantSearchCandidate(
    string CandidateToken,
    string DisplayName,
    DateOnly? DateOfBirth,
    string MaskedIdentification,
    string MaskedPhone);

public sealed record AssistantSearchResult(
    IReadOnlyList<AssistantSearchCandidate> Candidates,
    bool HasMore);

public sealed record AssistantCandidateReference(
    Guid PatientId,
    string PracticeId,
    string UserId,
    string AgentRunId,
    string DisplayName,
    string MaskedIdentification,
    string MaskedPhone);

public interface IAssistantPatientSearch
{
    Task<AssistantSearchResult> SearchAsync(
        string practiceId, string userId, string agentRunId, string search, DateOnly? birthDate, CancellationToken cancellationToken);

    Task<AssistantCandidateReference?> ResolveAsync(
        string practiceId, string userId, string agentRunId, string candidateToken, CancellationToken cancellationToken);
}

public interface IAssistantOperationsAccessVerifier
{
    Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken);
}
