using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class SecurityAuditQueryService(
    AuditDbContext dbContext,
    Vitarish.Requests.Application.Abstractions.IRequestContext requestContext,
    IAuditStorageSizeReader? storageSizeReader = null,
    IOptions<SecurityAuditOptions>? options = null,
    ISecurityAuditWriter? auditWriter = null) : ISecurityAuditQueryService
{
    public async Task<SecurityAuditPageReadDto> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        bool authorized = AuditAccessAuthorization.HasReviewAccess(requestContext);
        await RecordReviewAttemptAsync(authorized, cancellationToken);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);

        if (query.Page < 1 || query.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(query), "Audit review paging is outside the supported range.");
        if (query.FromUtc is { Kind: not DateTimeKind.Utc } || query.ToUtc is { Kind: not DateTimeKind.Utc })
            throw new ArgumentException("Audit review date filters must be UTC.", nameof(query));

        try
        {
            IQueryable<SecurityAuditEvent> filtered = ApplyFilters(dbContext.SecurityAuditEvents.AsNoTracking(), query);
            int totalCount = await filtered.CountAsync(cancellationToken);
            IQueryable<SecurityAuditEvent> sorted = ApplySort(filtered, query);
            List<SecurityAuditEventReadDto> items = await sorted
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToReadDto)
                .ToListAsync(cancellationToken);

            long storageSize = storageSizeReader is null ? 0 : await storageSizeReader.GetStorageSizeBytesAsync(cancellationToken);
            long threshold = options?.Value.CapacityWarningBytes ?? 0;
            return new SecurityAuditPageReadDto(items, query.Page, query.PageSize, totalCount, threshold > 0 && storageSize >= threshold);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SecurityAuditStoreException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SecurityAuditStoreException("The security audit store could not be read.", exception);
        }
    }

    public async Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .Where(e => e.TenantId == requestContext.TenantId && e.Resource == resource)
            .OrderByDescending(e => e.OccurredAt)
            .Select(ToReadDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .Where(e => e.TenantId == requestContext.TenantId && e.IdempotencyKey == idempotencyKey)
            .Select(ToReadDto)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task RecordReviewAttemptAsync(bool authorized, CancellationToken cancellationToken)
    {
        if (auditWriter is null || string.IsNullOrWhiteSpace(requestContext.TenantId))
            return;

        try
        {
            string requestId = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.AuditReviewEvent,
                EventCategory = SecurityAuditEventCatalog.ReviewCategory,
                Outcome = authorized ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                TenantId = requestContext.TenantId,
                Resource = requestContext.TenantId,
                Operation = SecurityAuditEventCatalog.AuditReviewOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestId,
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                IdempotencyKey = requestId + SecurityAuditEventCatalog.AuditReviewIdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 deliberately preserves the review result when its audit
            // record cannot be stored; the writer has emitted the host error.
        }
    }

    private IQueryable<SecurityAuditEvent> ApplyFilters(IQueryable<SecurityAuditEvent> source, SecurityAuditQuery query)
    {
        IQueryable<SecurityAuditEvent> result = source.Where(item => item.TenantId == requestContext.TenantId);
        if (!string.IsNullOrWhiteSpace(query.ActorId))
            result = result.Where(item => item.ActorId == query.ActorId);
        if (!string.IsNullOrWhiteSpace(query.EventName))
            result = result.Where(item => item.EventName == query.EventName);
        if (!string.IsNullOrWhiteSpace(query.EventCategory))
            result = result.Where(item => item.EventCategory == query.EventCategory);
        if (query.FromUtc is { } fromUtc)
            result = result.Where(item => item.OccurredAt >= fromUtc);
        if (query.ToUtc is { } toUtc)
            result = result.Where(item => item.OccurredAt <= toUtc);
        return result;
    }

    private static IQueryable<SecurityAuditEvent> ApplySort(IQueryable<SecurityAuditEvent> source, SecurityAuditQuery query)
    {
        IOrderedQueryable<SecurityAuditEvent> ordered = query.SortBy switch
        {
            SecurityAuditSortField.Actor => query.Descending
                ? source.OrderByDescending(item => item.ActorId).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.ActorId).ThenBy(item => item.OccurredAt),
            SecurityAuditSortField.EventType => query.Descending
                ? source.OrderByDescending(item => item.EventName).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.EventName).ThenBy(item => item.OccurredAt),
            _ => query.Descending
                ? source.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id)
                : source.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id)
        };
        return ordered;
    }

    private static Expression<Func<SecurityAuditEvent, SecurityAuditEventReadDto>> ToReadDto => item => new SecurityAuditEventReadDto
    {
        Id = item.Id,
        EventName = item.EventName,
        EventCategory = item.EventCategory,
        Outcome = item.Outcome,
        ActorKind = item.ActorKind,
        ActorId = item.ActorId,
        SubjectId = item.SubjectId,
        TenantId = item.TenantId,
        Resource = item.Resource,
        Operation = item.Operation,
        OccurredAt = item.OccurredAt,
        CorrelationId = item.CorrelationId,
        SourceBoundary = item.SourceBoundary,
        SourceIpKind = item.SourceIpKind,
        SourceIpAddress = item.SourceIpAddress,
        RedactedReason = item.RedactedReason
    };
}
