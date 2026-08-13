namespace Vitarish.Clinic.Application.Assistant;

/// <summary>Resolves the authenticated, authorized patient workspace context at the server boundary.</summary>
public interface IAssistantContextFactory
{
    Task<AssistantContext> CreateAsync(
        Guid routePatientId,
        string? conversationId,
        CancellationToken cancellationToken = default,
        string? agentKey = null);

    Task<AssistantContext> CreateOperationsAsync(
        string? conversationId,
        CancellationToken cancellationToken = default,
        string? agentKey = null);
}

public interface IAssistantRoutePatientResolver
{
    Task<bool> IsAvailableAsync(string practiceId, Guid patientId, CancellationToken cancellationToken);
}

/// <summary>Re-reads patient-workspace permissions at every signed runtime callback.</summary>
public interface IAssistantPatientAccessVerifier
{
    Task<AssistantPatientAccess> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken);
}

public sealed record AssistantPatientAccess(IReadOnlySet<string> Permissions, string Role);
