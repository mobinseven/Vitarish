using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetAuditLogQueryHandler(ClinicalDbContext dbContext)
    : IRequestHandler<GetAuditLogQuery, IReadOnlyList<AuditLogReadDto>>
{
    public async Task<IReadOnlyList<AuditLogReadDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        AuditLogFilter filter = request.Filter;
        IQueryable<Domain.Entities.ClinicalAuditEntry> query = dbContext.AuditEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            string category = filter.Category.Trim();
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            string action = filter.Action.Trim();
            query = query.Where(e => e.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorId))
        {
            string actorId = filter.ActorId.Trim();
            query = query.Where(e => e.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            string resourceType = filter.ResourceType.Trim();
            query = query.Where(e => e.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
        {
            string resourceId = filter.ResourceId.Trim();
            query = query.Where(e => e.ResourceId == resourceId);
        }

        if (filter.Since is not null)
        {
            DateTimeOffset since = filter.Since.Value;
            query = query.Where(e => e.CreatedAt >= since);
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .Select(e => new AuditLogReadDto
            {
                Id = e.Id,
                Category = e.Category,
                Action = e.Action,
                ActorId = e.ActorId,
                ResourceType = e.ResourceType,
                ResourceId = e.ResourceId,
                CorrelationId = e.CorrelationId,
                Reason = e.Reason,
                Outcome = e.Outcome,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
