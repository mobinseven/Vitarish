namespace Vitarish.Clinic.Application.PatientAgent.Operations;

public static class PatientAgentOperationsScope
{
    public const string TenantOwnerRole = "TenantOwner";
    public const string PlatformAdminRole = "PlatformAdmin";

    public static bool IsAuthorizedRole(string? role) =>
        string.Equals(role, TenantOwnerRole, StringComparison.Ordinal) ||
        string.Equals(role, PlatformAdminRole, StringComparison.Ordinal);
}

public sealed record PatientAgentSearchCandidate(
    string CandidateToken,
    string DisplayName,
    DateOnly? DateOfBirth,
    string MaskedIdentification,
    string MaskedPhone);

public sealed record PatientAgentSearchResult(
    IReadOnlyList<PatientAgentSearchCandidate> Candidates,
    bool HasMore);

public sealed record PatientAgentCandidateReference(
    Guid PatientId,
    string PracticeId,
    string UserId,
    string AgentRunId,
    string DisplayName,
    string MaskedIdentification,
    string MaskedPhone);

public interface IPatientAgentPatientSearch
{
    Task<PatientAgentSearchResult> SearchAsync(
        string practiceId, string userId, string agentRunId, string search, DateOnly? birthDate, CancellationToken cancellationToken);

    Task<PatientAgentCandidateReference?> ResolveAsync(
        string practiceId, string userId, string agentRunId, string candidateToken, CancellationToken cancellationToken);
}

public interface IPatientAgentOperationsAccessVerifier
{
    Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken);
}
