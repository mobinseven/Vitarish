using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class RegisterPatientCommandHandler(
    IClinicalActionService actionService,
    IRequestContext requestContext)
    : IRequestHandler<RegisterPatientCommand, ClinicalActionResult>
{
    public Task<ClinicalActionResult> Handle(RegisterPatientCommand request, CancellationToken cancellationToken) =>
        actionService.DispatchAsync(new ClinicalActionRequest
        {
            ActorId = request.ActorId,
            TenantId = request.TenantId,
            PatientFhirReference = string.Empty,
            ActionType = ClinicalActionType.RegisterPatient,
            Payload = request.Payload,
            AccessReason = request.AccessReason
        }, null, cancellationToken);
}
