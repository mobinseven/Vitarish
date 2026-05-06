namespace Vitarish.Clinical.Domain.Abstractions;

public interface IWorkflowEventPublisher
{
    Task PublishPatientContextOpenedAsync(string patientRef, string actorId, string tenantId, CancellationToken cancellationToken = default);
    Task PublishPatientRegisteredAsync(string patientFhirId, string actorId, string tenantId, CancellationToken cancellationToken = default);
    Task PublishVisitWorkflowCreatedAsync(Guid workflowId, string patientRef, string visitType, string tenantId, CancellationToken cancellationToken = default);
    Task PublishVisitCheckedInAsync(Guid workflowId, string patientRef, string actorId, CancellationToken cancellationToken = default);
    Task PublishEncounterStartedAsync(Guid workflowId, string encounterFhirId, string patientRef, CancellationToken cancellationToken = default);
    Task PublishVitalsRecordedAsync(Guid workflowId, IReadOnlyList<string> observationRefs, string actorId, CancellationToken cancellationToken = default);
    Task PublishDiagnosisAddedAsync(Guid workflowId, string conditionFhirId, string actorId, CancellationToken cancellationToken = default);
    Task PublishMedicationRequestedAsync(Guid workflowId, string medicationRequestFhirId, string actorId, CancellationToken cancellationToken = default);
    Task PublishClinicalNoteSignedAsync(Guid workflowId, string documentFhirId, string actorId, CancellationToken cancellationToken = default);
    Task PublishVisitCompletedAsync(Guid workflowId, string encounterFhirId, string patientRef, string tenantId, CancellationToken cancellationToken = default);
    Task PublishWorkflowCancelledAsync(Guid workflowId, string reason, string actorId, string tenantId, CancellationToken cancellationToken = default);
    Task PublishFhirCommitFailedAsync(Guid workflowId, string actionType, string failureKind, string correlationId, CancellationToken cancellationToken = default);
}
