namespace Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

public interface IPrescriptionAgentCapability
{
    Task<PrescriptionTemplateSearchResult> SearchAsync(string tenantId, string requestedName, CancellationToken cancellationToken);
    Task<PrescriptionTemplateRequirements?> GetRequirementsAsync(Guid templateId, string tenantId, CancellationToken cancellationToken);
    Task<PrescriptionDraftPreparationResult> PrepareAsync(Guid templateId, string? insuranceCoverage, PrescriptionAgentScope scope, PrescriptionAgentProvenance provenance, CancellationToken cancellationToken);
    Task<PrescriptionConfirmationResult> ConfirmAsync(Guid reviewId, PrescriptionAgentScope scope, string confirmedByUserId, CancellationToken cancellationToken);
}

public sealed class PrescriptionAgentCapability(
    SearchPrescriptionTemplatesForAgent search,
    GetPrescriptionTemplateRequirements requirements,
    PreparePrescriptionDraftFromTemplate prepare,
    ConfirmPrescriptionDraft confirm) : IPrescriptionAgentCapability
{
    public Task<PrescriptionTemplateSearchResult> SearchAsync(string tenantId, string requestedName, CancellationToken cancellationToken) => search.ExecuteAsync(tenantId, requestedName, cancellationToken);
    public Task<PrescriptionTemplateRequirements?> GetRequirementsAsync(Guid templateId, string tenantId, CancellationToken cancellationToken) => requirements.ExecuteAsync(templateId, tenantId, cancellationToken);
    public Task<PrescriptionDraftPreparationResult> PrepareAsync(Guid templateId, string? insuranceCoverage, PrescriptionAgentScope scope, PrescriptionAgentProvenance provenance, CancellationToken cancellationToken) => prepare.ExecuteAsync(templateId, insuranceCoverage, scope, provenance, cancellationToken);
    public Task<PrescriptionConfirmationResult> ConfirmAsync(Guid reviewId, PrescriptionAgentScope scope, string confirmedByUserId, CancellationToken cancellationToken) => confirm.ExecuteAsync(reviewId, scope, confirmedByUserId, cancellationToken);
}
