using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetIncompleteVisitSummaryQueryHandler(ClinicalDbContext dbContext)
    : IRequestHandler<GetIncompleteVisitSummaryQuery, IncompleteVisitSummary>
{
    public async Task<IncompleteVisitSummary> Handle(GetIncompleteVisitSummaryQuery request, CancellationToken cancellationToken)
    {
        List<(VisitWorkflowStatus Status, int Count)> counts = await dbContext.ClinicalWorkflows
            .AsNoTracking()
            .Where(w => w.Status != VisitWorkflowStatus.Completed && w.Status != VisitWorkflowStatus.Cancelled)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(x => (x.Status, x.Count)).ToList(), cancellationToken);

        DateTimeOffset staleThreshold = DateTimeOffset.UtcNow.AddHours(-24);
        int staleCount = await dbContext.ClinicalWorkflows
            .AsNoTracking()
            .CountAsync(w =>
                w.Status != VisitWorkflowStatus.Completed &&
                w.Status != VisitWorkflowStatus.Cancelled &&
                w.CreatedAt < staleThreshold,
                cancellationToken);

        return new IncompleteVisitSummary
        {
            TotalOpen = counts.Where(c => c.Status == VisitWorkflowStatus.CheckedIn).Sum(c => c.Count),
            TotalInTriage = counts.Where(c => c.Status == VisitWorkflowStatus.InTriage).Sum(c => c.Count),
            TotalReadyForProvider = counts.Where(c => c.Status == VisitWorkflowStatus.ReadyForProvider).Sum(c => c.Count),
            TotalInProgress = counts.Where(c => c.Status == VisitWorkflowStatus.InProgress).Sum(c => c.Count),
            TotalSigned = counts.Where(c => c.Status == VisitWorkflowStatus.Signed).Sum(c => c.Count),
            TotalStale = staleCount
        };
    }
}
