using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class SecurityAuditQueryService(AuditDbContext dbContext, Vitarish.Requests.Application.Abstractions.IRequestContext requestContext) : ISecurityAuditQueryService
{
    public async Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .Where(e => e.Resource == resource)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new SecurityAuditEventReadDto
            {
                Id = e.Id,
                EventName = e.EventName,
                Outcome = e.Outcome,
                ActorId = e.ActorId,
                SubjectId = e.SubjectId,
                TenantId = e.TenantId,
                Resource = e.Resource,
                Operation = e.Operation,
                OccurredAt = e.OccurredAt,
                CorrelationId = e.CorrelationId,
                SourceBoundary = e.SourceBoundary,
                RedactedReason = e.RedactedReason
            }).ToListAsync(cancellationToken);
    }

    public async Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .Where(e => e.IdempotencyKey == idempotencyKey)
            .Select(e => new SecurityAuditEventReadDto
            {
                Id = e.Id,
                EventName = e.EventName,
                Outcome = e.Outcome,
                ActorId = e.ActorId,
                SubjectId = e.SubjectId,
                TenantId = e.TenantId,
                Resource = e.Resource,
                Operation = e.Operation,
                OccurredAt = e.OccurredAt,
                CorrelationId = e.CorrelationId,
                SourceBoundary = e.SourceBoundary,
                RedactedReason = e.RedactedReason
            }).SingleOrDefaultAsync(cancellationToken);
    }
}
