namespace Vitarish.Clinic.Application.PatientAgent;

/// <summary>Resolves the authenticated, authorized patient workspace context at the server boundary.</summary>
public interface IPatientAgentContextFactory
{
    Task<PatientAgentContext> CreateAsync(
        Guid routePatientId,
        string? conversationId,
        CancellationToken cancellationToken = default);

    Task<PatientAgentContext> CreateOperationsAsync(
        string? conversationId,
        CancellationToken cancellationToken = default);
}

public interface IPatientAgentRoutePatientResolver
{
    Task<bool> IsAvailableAsync(string practiceId, Guid patientId, CancellationToken cancellationToken);
}
