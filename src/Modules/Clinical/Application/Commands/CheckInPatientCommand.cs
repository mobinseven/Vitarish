using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record CheckInPatientCommand(
    Guid WorkflowId,
    string ActorId,
    string? VisitType = null) : IRequest<ClinicalActionResult>;
