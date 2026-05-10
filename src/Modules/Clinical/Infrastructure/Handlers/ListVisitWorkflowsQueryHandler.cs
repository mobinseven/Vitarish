using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ListVisitWorkflowsQueryHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<ListVisitWorkflowsQuery, IReadOnlyList<VisitWorkflowReadDto>>
{
    public Task<IReadOnlyList<VisitWorkflowReadDto>> Handle(ListVisitWorkflowsQuery request, CancellationToken cancellationToken) =>
        workflowService.ListAsync(request.Filter, cancellationToken);
}
