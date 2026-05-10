using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetWorkflowStateHistoryQueryHandler(ClinicalDbContext dbContext)
    : IRequestHandler<GetWorkflowStateHistoryQuery, IReadOnlyList<WorkflowStateHistoryReadDto>>
{
    public async Task<IReadOnlyList<WorkflowStateHistoryReadDto>> Handle(GetWorkflowStateHistoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.WorkflowStateHistories
            .AsNoTracking()
            .Where(h => h.WorkflowId == request.WorkflowId)
            .OrderBy(h => h.OccurredAt)
            .Select(h => new WorkflowStateHistoryReadDto
            {
                Id = h.Id,
                WorkflowId = h.WorkflowId,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ActorId = h.ActorId,
                Reason = h.Reason,
                OccurredAt = h.OccurredAt
            })
            .ToListAsync(cancellationToken);
    }
}
