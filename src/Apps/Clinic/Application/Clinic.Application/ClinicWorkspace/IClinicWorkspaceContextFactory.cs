namespace Vitarish.Clinic.Application.ClinicWorkspace;

/// <summary>Resolves the authenticated, authorized patient workspace context at the server boundary.</summary>
public interface IClinicWorkspaceContextFactory
{
    Task<ClinicWorkspaceContext> CreateAsync(
        Guid routePatientId,
        string? conversationId,
        CancellationToken cancellationToken = default,
        string? agentKey = null);

    Task<ClinicWorkspaceContext> CreateOperationsAsync(
        string? conversationId,
        CancellationToken cancellationToken = default,
        string? agentKey = null);
}

public interface IClinicWorkspaceRoutePatientResolver
{
    Task<bool> IsAvailableAsync(string practiceId, Guid patientId, CancellationToken cancellationToken);
}

/// <summary>Re-reads patient-workspace permissions at every signed runtime callback.</summary>
public interface IClinicWorkspacePatientAccessVerifier
{
    Task<ClinicWorkspacePatientAccess> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken);
}

public sealed record ClinicWorkspacePatientAccess(IReadOnlySet<string> Permissions, string Role);
