namespace Vitarish.Clinic.Application.PatientAgent;

/// <summary>Resolves the authenticated, authorized patient workspace context at the server boundary.</summary>
public interface IPatientAgentContextFactory
{
    Task<PatientAgentContext> CreateAsync(
        Guid routePatientId,
        string? conversationId,
        CancellationToken cancellationToken = default);
}
