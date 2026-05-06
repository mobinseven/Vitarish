using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record AddDiagnosisCommand(
    Guid WorkflowId,
    DiagnosisPayload Payload,
    string ActorId) : IRequest<ClinicalActionResult>;
