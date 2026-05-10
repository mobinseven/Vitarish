using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record StartEncounterCommand(
    Guid WorkflowId,
    StartEncounterPayload Payload,
    string ActorId) : IRequest<ClinicalActionResult>;
