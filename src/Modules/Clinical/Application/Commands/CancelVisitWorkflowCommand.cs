using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record CancelVisitWorkflowCommand(
    Guid WorkflowId,
    string ActorId,
    string Reason) : IRequest<VisitWorkflowReadDto>;
