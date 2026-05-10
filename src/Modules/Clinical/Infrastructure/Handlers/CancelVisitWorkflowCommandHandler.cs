using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class CancelVisitWorkflowCommandHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<CancelVisitWorkflowCommand, VisitWorkflowReadDto>
{
    public Task<VisitWorkflowReadDto> Handle(CancelVisitWorkflowCommand request, CancellationToken cancellationToken) =>
        workflowService.CancelAsync(request.WorkflowId, request.ActorId, request.Reason, cancellationToken);
}
