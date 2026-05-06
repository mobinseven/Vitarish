using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class TransitionWorkflowStatusCommandHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<TransitionWorkflowStatusCommand, VisitWorkflowReadDto>
{
    public Task<VisitWorkflowReadDto> Handle(TransitionWorkflowStatusCommand request, CancellationToken cancellationToken) =>
        workflowService.TransitionAsync(request.WorkflowId, request.TargetStatus, request.ActorId, request.Reason, cancellationToken);
}
