using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ReopenVisitWorkflowCommandHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<ReopenVisitWorkflowCommand, VisitWorkflowReadDto>
{
    public Task<VisitWorkflowReadDto> Handle(ReopenVisitWorkflowCommand request, CancellationToken cancellationToken) =>
        workflowService.ReopenAsync(request.WorkflowId, request.ActorId, request.Reason, cancellationToken);
}
