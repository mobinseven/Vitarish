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
    IOptions<SecurityAuditOptions>? options = null) : ISecurityAuditQueryService
{
    public async Task<SecurityAuditPageReadDto> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);

        ValidateQuery(query);
        return await ExecutePageAsync(
            () => ApplyFilters(dbContext.SecurityAuditEvents.AsNoTracking(), query, requestContext.TenantId, excludePlatformScope: true, tenantScopeRequired: true),
            query,
            cancellationToken);
    }

    public async Task<SecurityAuditPageReadDto> QueryPlatformAsync(SecurityAuditPlatformQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AuditAccessAuthorization.DemandPlatformReviewAccess(requestContext);

        ValidateQuery(request.Query);
        ValidatePlatformQuery(request);

        return await ExecutePageAsync(
            () => ApplyFilters(dbContext.SecurityAuditEvents.AsNoTracking().IgnoreQueryFilters(), request.Query, request.TenantId, request.UncataloguedOnly),
            request.Query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditCatalogEntryReadDto>> GetPlatformCatalogAsync(CancellationToken cancellationToken)
    {
        AuditAccessAuthorization.DemandPlatformReviewAccess(requestContext);
        IReadOnlyList<SecurityAuditCatalogEntryReadDto> catalog = SecurityAuditEventCatalog.Entries
            .Where(entry => entry.Applicable)
            .Select(entry => new SecurityAuditCatalogEntryReadDto(
                entry.Category,
                entry.EventName,
                entry.Operation,
                entry.Operation == SecurityAuditEventCatalog.OperationWildcard))
            .ToArray();
        return catalog;
    }

    public async Task<IReadOnlyList<SecurityAuditEventReadDto>> ExportPlatformAsync(SecurityAuditPlatformQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AuditAccessAuthorization.DemandPlatformReviewAccess(requestContext);

        ValidateQuery(request.Query);
        ValidatePlatformQuery(request);

        try
        {
            List<SecurityAuditEventReadDto> rows = await ApplySort(
                    ApplyFilters(dbContext.SecurityAuditEvents.AsNoTracking().IgnoreQueryFilters(), request.Query, request.TenantId, request.UncataloguedOnly),
                    request.Query)
                .Take(SecurityAuditExportPolicy.ProbeRows)
                .Select(ToReadDto)
                .ToListAsync(cancellationToken);

            if (rows.Count > SecurityAuditExportPolicy.MaximumRows)
                throw new SecurityAuditExportLimitException();

            return rows;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SecurityAuditExportLimitException)
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

        try
        {
            return await dbContext.SecurityAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == requestContext.TenantId && e.TenantId != SecurityAuditScopeCatalog.PlatformScopeId && e.Resource == resource)
                .OrderByDescending(e => e.OccurredAt)
                .Select(ToReadDto)
                .ToListAsync(cancellationToken);
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

    public async Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        AuditAccessAuthorization.DemandReviewAccess(requestContext);

        try
        {
            return await dbContext.SecurityAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == requestContext.TenantId && e.TenantId != SecurityAuditScopeCatalog.PlatformScopeId && e.IdempotencyKey == idempotencyKey)
                .Select(ToReadDto)
                .SingleOrDefaultAsync(cancellationToken);
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

    private async Task<SecurityAuditPageReadDto> ExecutePageAsync(
        Func<IQueryable<SecurityAuditEvent>> filteredFactory,
        SecurityAuditQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<SecurityAuditEvent> filtered = filteredFactory();
            int totalCount = await filtered.CountAsync(cancellationToken);
            List<SecurityAuditEventReadDto> items = await ApplySort(filtered, query)
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

    private static void ValidateQuery(SecurityAuditQuery query)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(query), "Audit review paging is outside the supported range.");
        if (query.FromUtc is { Kind: not DateTimeKind.Utc } || query.ToUtc is { Kind: not DateTimeKind.Utc })
            throw new ArgumentException("Audit review date filters must be UTC.", nameof(query));
        if (!string.IsNullOrWhiteSpace(query.EventId) && !Guid.TryParse(query.EventId, out _))
            throw new ArgumentException("Audit event ID must be a valid identifier.", nameof(query));
    }

    private static void ValidatePlatformQuery(SecurityAuditPlatformQuery request)
    {
        if (string.Equals(request.TenantId, SecurityAuditScopeCatalog.PlatformScopeId, StringComparison.Ordinal))
            throw new ArgumentException("The platform audit scope is not a tenant filter.", nameof(request));
        if (request.TenantId is not null && request.TenantId.Length > 64)
            throw new ArgumentException("The audit tenant filter is too long.", nameof(request));
        if (request.UncataloguedOnly && !string.IsNullOrWhiteSpace(request.Query.EventCategory))
            throw new ArgumentException("Uncatalogued review cannot be combined with a catalog category filter.", nameof(request));
    }

    private static IQueryable<SecurityAuditEvent> ApplyFilters(
        IQueryable<SecurityAuditEvent> source,
        SecurityAuditQuery query,
        string? tenantId,
        bool uncataloguedOnly = false,
        bool excludePlatformScope = false,
        bool tenantScopeRequired = false)
    {
        IQueryable<SecurityAuditEvent> result = source;
        if (tenantScopeRequired && string.IsNullOrWhiteSpace(tenantId))
            return result.Where(_ => false);
        if (tenantScopeRequired || !string.IsNullOrWhiteSpace(tenantId))
            result = result.Where(item => item.TenantId == tenantId);
        if (excludePlatformScope)
            result = result.Where(item => item.TenantId != SecurityAuditScopeCatalog.PlatformScopeId);
        if (!string.IsNullOrWhiteSpace(query.ActorId))
            result = result.Where(item => item.ActorId == query.ActorId);
        if (!string.IsNullOrWhiteSpace(query.SubjectId))
            result = result.Where(item => item.SubjectId == query.SubjectId);
        if (!string.IsNullOrWhiteSpace(query.EventId) && Guid.TryParse(query.EventId, out Guid eventId))
            result = result.Where(item => item.Id == eventId);
        if (!string.IsNullOrWhiteSpace(query.EventName))
            result = result.Where(item => item.EventName == query.EventName);
        if (!string.IsNullOrWhiteSpace(query.EventCategory))
            result = result.Where(item => item.EventCategory == query.EventCategory);
        if (!string.IsNullOrWhiteSpace(query.Operation))
            result = result.Where(item => item.Operation == query.Operation);
        if (!string.IsNullOrWhiteSpace(query.Outcome))
            result = result.Where(item => item.Outcome == query.Outcome);
        if (query.FromUtc is { } fromUtc)
            result = result.Where(item => item.OccurredAt >= fromUtc);
        if (query.ToUtc is { } toUtc)
            result = result.Where(item => item.OccurredAt <= toUtc);
        if (uncataloguedOnly)
        {
            Expression<Func<SecurityAuditEvent, bool>> catalogued = SecurityAuditEventCatalog.CataloguedPredicate();
            result = result.Where(Expression.Lambda<Func<SecurityAuditEvent, bool>>(
                Expression.Not(catalogued.Body),
                catalogued.Parameters.ToArray()));
        }
        return result;
    }

    private static IQueryable<SecurityAuditEvent> ApplySort(IQueryable<SecurityAuditEvent> source, SecurityAuditQuery query)
    {
        IOrderedQueryable<SecurityAuditEvent> ordered = query.SortBy switch
        {
            SecurityAuditSortField.Actor => query.Descending
                ? source.OrderByDescending(item => item.ActorId).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.ActorId).ThenBy(item => item.OccurredAt),
            SecurityAuditSortField.ActorKind => query.Descending
                ? source.OrderByDescending(item => item.ActorKind).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.ActorKind).ThenBy(item => item.OccurredAt),
            SecurityAuditSortField.SourceIpKind => query.Descending
                ? source.OrderByDescending(item => item.SourceIpKind).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.SourceIpKind).ThenBy(item => item.OccurredAt),
            SecurityAuditSortField.EventType => query.Descending
                ? source.OrderByDescending(item => item.EventName).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.EventName).ThenBy(item => item.OccurredAt),
            SecurityAuditSortField.SourceIpAddress => query.Descending
                ? source.OrderByDescending(item => item.SourceIpAddress).ThenByDescending(item => item.OccurredAt)
                : source.OrderBy(item => item.SourceIpAddress).ThenBy(item => item.OccurredAt),
            _ => query.Descending
                ? source.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id)
                : source.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id)
        };
        return ordered;
    }

    internal static Expression<Func<SecurityAuditEvent, SecurityAuditEventReadDto>> ToReadDto => item => new SecurityAuditEventReadDto
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
        ResourceNameHash = item.ResourceNameHash,
        ResourceExtension = item.ResourceExtension,
        ResourceSize = item.ResourceSize,
        Operation = item.Operation,
        OccurredAt = item.OccurredAt,
        CorrelationId = item.CorrelationId,
        SourceBoundary = item.SourceBoundary,
        SourceIpKind = item.SourceIpKind,
        SourceIpAddress = item.SourceIpAddress,
        ClientInterfaceHost = item.ClientInterfaceHost,
        RedactedReason = item.RedactedReason
    };
}
