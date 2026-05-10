using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record TransitionWorkflowStatusCommand(
    Guid WorkflowId,
    VisitWorkflowStatus TargetStatus,
    string ActorId,
    string? Reason = null) : IRequest<VisitWorkflowReadDto>;
