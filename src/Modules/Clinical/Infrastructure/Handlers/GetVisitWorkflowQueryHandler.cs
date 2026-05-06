using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetVisitWorkflowQueryHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<GetVisitWorkflowQuery, VisitWorkflowReadDto?>
{
    public Task<VisitWorkflowReadDto?> Handle(GetVisitWorkflowQuery request, CancellationToken cancellationToken) =>
        workflowService.GetAsync(request.WorkflowId, cancellationToken);
}
