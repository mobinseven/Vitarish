using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record RecordVitalsCommand(
    Guid WorkflowId,
    VitalsPayload Payload,
    string ActorId) : IRequest<ClinicalActionResult>;
