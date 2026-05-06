using MediatR;
using Vitarish.Clinical.Application.Events;
using Vitarish.Clinical.Domain.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class WorkflowEventPublisher(IMediator mediator) : IWorkflowEventPublisher
{
    public Task PublishPatientContextOpenedAsync(string patientRef, string actorId, string tenantId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new PatientContextOpenedEvent(patientRef, actorId, tenantId), cancellationToken);

    public Task PublishPatientRegisteredAsync(string patientFhirId, string actorId, string tenantId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new PatientRegisteredEvent(patientFhirId, actorId, tenantId), cancellationToken);

    public Task PublishVisitWorkflowCreatedAsync(Guid workflowId, string patientRef, string visitType, string tenantId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new VisitWorkflowCreatedEvent(workflowId, patientRef, visitType, tenantId), cancellationToken);

    public Task PublishVisitCheckedInAsync(Guid workflowId, string patientRef, string actorId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new VisitCheckedInEvent(workflowId, patientRef, actorId), cancellationToken);

    public Task PublishEncounterStartedAsync(Guid workflowId, string encounterFhirId, string patientRef, CancellationToken cancellationToken = default) =>
        mediator.Publish(new EncounterStartedEvent(workflowId, encounterFhirId, patientRef), cancellationToken);

    public Task PublishVitalsRecordedAsync(Guid workflowId, IReadOnlyList<string> observationRefs, string actorId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new VitalsRecordedEvent(workflowId, observationRefs, actorId), cancellationToken);

    public Task PublishDiagnosisAddedAsync(Guid workflowId, string conditionFhirId, string actorId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new DiagnosisAddedEvent(workflowId, conditionFhirId, actorId), cancellationToken);

    public Task PublishMedicationRequestedAsync(Guid workflowId, string medicationRequestFhirId, string actorId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new MedicationRequestedEvent(workflowId, medicationRequestFhirId, actorId), cancellationToken);

    public Task PublishClinicalNoteSignedAsync(Guid workflowId, string documentFhirId, string actorId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new ClinicalNoteSignedEvent(workflowId, documentFhirId, actorId), cancellationToken);

    public Task PublishVisitCompletedAsync(Guid workflowId, string encounterFhirId, string patientRef, string tenantId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new VisitCompletedEvent(workflowId, encounterFhirId, patientRef, tenantId), cancellationToken);

    public Task PublishWorkflowCancelledAsync(Guid workflowId, string reason, string actorId, string tenantId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new WorkflowCancelledEvent(workflowId, reason, actorId, tenantId), cancellationToken);

    public Task PublishFhirCommitFailedAsync(Guid workflowId, string actionType, string failureKind, string correlationId, CancellationToken cancellationToken = default) =>
        mediator.Publish(new FhirCommitFailedEvent(workflowId, actionType, failureKind, correlationId), cancellationToken);
}
