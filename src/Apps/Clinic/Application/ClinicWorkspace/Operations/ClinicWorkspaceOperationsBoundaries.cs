namespace Vitarish.Clinic.Application.ClinicWorkspace.Operations;

public static class ClinicWorkspaceOperationsScope
{
    public const string TenantOwnerRole = "TenantOwner";
    public const string PlatformAdminRole = "PlatformAdmin";

    public static bool IsAuthorizedRole(string? role) =>
        string.Equals(role, TenantOwnerRole, StringComparison.Ordinal) ||
        string.Equals(role, PlatformAdminRole, StringComparison.Ordinal);
}

public sealed record ClinicWorkspaceSearchCandidate(
    string CandidateToken,
    string DisplayName,
    DateOnly? DateOfBirth,
    string MaskedIdentification,
    string MaskedPhone);

public sealed record ClinicWorkspaceSearchResult(
    IReadOnlyList<ClinicWorkspaceSearchCandidate> Candidates,
    bool HasMore);

public sealed record ClinicWorkspaceCandidateReference(
    Guid PatientId,
    string PracticeId,
    string UserId,
    string AgentRunId,
    string DisplayName,
    string MaskedIdentification,
    string MaskedPhone);

public interface IClinicWorkspacePatientSearch
{
    Task<ClinicWorkspaceSearchResult> SearchAsync(
        string practiceId, string userId, string agentRunId, string search, DateOnly? birthDate, CancellationToken cancellationToken);

    Task<ClinicWorkspaceCandidateReference?> ResolveAsync(
        string practiceId, string userId, string agentRunId, string candidateToken, CancellationToken cancellationToken);
}

public interface IClinicWorkspaceOperationsAccessVerifier
{
    Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken);
}
