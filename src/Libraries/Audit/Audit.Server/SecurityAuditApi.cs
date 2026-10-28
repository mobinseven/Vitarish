using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Audit.Server;

public static class SecurityAuditApi
{
    public static IEndpointRouteBuilder MapSecurityAuditApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapProtectedModuleGroup(SecurityAuditApiRoutes.Base, requireTenantAccess: false)
            .WithTags(SecurityAuditApiRoutes.DocumentTag)
            .WithMetadata(new AuditReviewEndpoint())
            .AddEndpointFilter<AuditReviewDenialFilter>();

        _ = group.MapModuleGet(
            SecurityAuditApiRoutes.SecurityEvents,
            GetSecurityEventsAsync,
            SecurityAuditApiOperations.ListSecurityEvents,
            typeof(SecurityAuditPageReadDto))
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        _ = group.MapModuleGet(
            SecurityAuditApiRoutes.PlatformSecurityEvents,
            GetPlatformSecurityEventsAsync,
            SecurityAuditApiOperations.ListPlatformSecurityEvents,
            typeof(SecurityAuditPageReadDto))
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        _ = group.MapModuleGet(
            SecurityAuditApiRoutes.PlatformSecurityEventCatalog,
            GetPlatformSecurityEventCatalogAsync,
            SecurityAuditApiOperations.GetPlatformSecurityEventCatalog,
            typeof(SecurityAuditCatalogEntryReadDto[]));

        _ = group.MapModuleGet(
            SecurityAuditApiRoutes.PlatformSecurityEventsExport,
            ExportPlatformSecurityEventsAsync,
            SecurityAuditApiOperations.ExportPlatformSecurityEvents,
            typeof(Stream),
            profile: ModuleEndpointProfile.BinaryDownload,
            contentType: SecurityAuditApiRoutes.CsvContentType)
            .ProducesProblem(StatusCodes.Status413RequestEntityTooLarge)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> GetSecurityEventsAsync(
        int page,
        int pageSize,
        string? actorId,
        string? eventName,
        string? eventCategory,
        DateTime? fromUtc,
        DateTime? toUtc,
        SecurityAuditSortField sortBy,
        bool descending,
        string? operation,
        string? outcome,
        string? eventId,
        ISecurityAuditQueryService queryService,
        CancellationToken cancellationToken)
    {
        try
        {
            SecurityAuditPageReadDto result = await queryService.QueryAsync(
                new SecurityAuditQuery(page, pageSize, actorId, eventName, eventCategory, fromUtc, toUtc, sortBy, descending, operation, outcome, eventId),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid audit review query.", detail: "The audit review query is invalid.");
        }
        catch (SecurityAuditStoreException)
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Audit review is unavailable.", detail: "The security audit store could not be read.");
        }
    }

    private static async Task<IResult> GetPlatformSecurityEventsAsync(
        int page,
        int pageSize,
        string? tenantId,
        string? actorId,
        string? eventName,
        string? eventCategory,
        DateTime? fromUtc,
        DateTime? toUtc,
        SecurityAuditSortField sortBy,
        bool descending,
        string? operation,
        string? outcome,
        bool uncataloguedOnly,
        string? eventId,
        string? subjectId,
        ISecurityAuditQueryService queryService,
        CancellationToken cancellationToken)
    {
        try
        {
            SecurityAuditPageReadDto result = await queryService.QueryPlatformAsync(
                new SecurityAuditPlatformQuery(
                    new SecurityAuditQuery(page, pageSize, actorId, eventName, eventCategory, fromUtc, toUtc, sortBy, descending, operation, outcome, eventId, subjectId),
                    tenantId,
                    uncataloguedOnly),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid global audit review query.", detail: "The global audit review query is invalid.");
        }
        catch (SecurityAuditStoreException)
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Audit review is unavailable.", detail: "The security audit store could not be read.");
        }
    }

    private static async Task<IResult> GetPlatformSecurityEventCatalogAsync(
        ISecurityAuditQueryService queryService,
        CancellationToken cancellationToken) =>
        Results.Ok(await queryService.GetPlatformCatalogAsync(cancellationToken));

    private static async Task<IResult> ExportPlatformSecurityEventsAsync(
        string? tenantId,
        string? actorId,
        string? eventName,
        string? eventCategory,
        DateTime? fromUtc,
        DateTime? toUtc,
        SecurityAuditSortField sortBy,
        bool descending,
        string? operation,
        string? outcome,
        bool uncataloguedOnly,
        string? eventId,
        string? subjectId,
        HttpContext httpContext,
        ISecurityAuditQueryService queryService,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<SecurityAuditEventReadDto> rows = await queryService.ExportPlatformAsync(
                new SecurityAuditPlatformQuery(
                    new SecurityAuditQuery(
                        ActorId: actorId,
                        EventName: eventName,
                        EventCategory: eventCategory,
                        FromUtc: fromUtc,
                        ToUtc: toUtc,
                        SortBy: sortBy,
                        Descending: descending,
                        Operation: operation,
                        Outcome: outcome,
                        EventId: eventId,
                        SubjectId: subjectId),
                    tenantId,
                    uncataloguedOnly),
                cancellationToken);
            httpContext.Response.Headers.CacheControl = SecurityAuditApiRoutes.NoStoreCacheControl;
            return Results.File(SecurityAuditCsvFormatter.Format(rows), SecurityAuditApiRoutes.CsvContentType);
        }
        catch (SecurityAuditExportLimitException)
        {
            return Results.Problem(statusCode: StatusCodes.Status413RequestEntityTooLarge, title: "Audit export is too large.", detail: "Narrow the filters to export no more than the supported number of audit events.");
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid audit export query.", detail: "The audit export query is invalid.");
        }
        catch (SecurityAuditStoreException)
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Audit export is unavailable.", detail: "The security audit store could not be read.");
        }
    }
}
