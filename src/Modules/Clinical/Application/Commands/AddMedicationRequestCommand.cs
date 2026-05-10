using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record AddMedicationRequestCommand(
    Guid WorkflowId,
    MedicationRequestPayload Payload,
    string ActorId) : IRequest<ClinicalActionResult>;
