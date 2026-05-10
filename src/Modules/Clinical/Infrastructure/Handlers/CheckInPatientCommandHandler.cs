using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class CheckInPatientCommandHandler(
    IClinicalActionService actionService,
    IVisitWorkflowService workflowService,
    IRequestContext requestContext)
    : IRequestHandler<CheckInPatientCommand, ClinicalActionResult>
{
    public async Task<ClinicalActionResult> Handle(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        VisitWorkflowReadDto? workflow = await workflowService.GetAsync(request.WorkflowId, cancellationToken);
        if (workflow is null)
        {
            return ClinicalActionResult.Fail($"Workflow '{request.WorkflowId}' not found.");
        }

        return await actionService.DispatchAsync(new ClinicalActionRequest
        {
            ActorId = request.ActorId,
            TenantId = requestContext.TenantId ?? string.Empty,
            PatientFhirReference = workflow.PatientFhirReference,
            EncounterFhirReference = workflow.EncounterFhirReference,
            ActionType = ClinicalActionType.CheckInPatient
        }, request.WorkflowId, cancellationToken);
    }
}
