using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record AddClinicalNoteCommand(
    Guid WorkflowId,
    ClinicalNotePayload Payload,
    string ActorId) : IRequest<ClinicalActionResult>;
