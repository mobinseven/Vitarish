using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

/// <summary>
/// Records reads of the security audit at the review API boundary: each unauthorized attempt, and each
/// successful view of an event list. The review query itself stays side-effect-free.
/// </summary>
internal sealed class AuditReviewFilter(AuditReviewRecorder recorder) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        object? result;
        try
        {
            result = await next(context);
        }
        catch (UnauthorizedAccessException)
        {
            await recorder.RecordDeniedAsync(context.HttpContext).ConfigureAwait(false);
            return Results.Forbid();
        }

        if (result is IStatusCodeHttpResult { StatusCode: StatusCodes.Status200OK })
            await recorder.RecordViewedAsync(context.HttpContext).ConfigureAwait(false);
        return result;
    }
}

internal sealed class AuditReviewRecorder(
    ISecurityAuditWriter writer,
    IRequestContext requestContext,
    IOptions<SecurityAuditOptions> options,
    TimeProvider timeProvider,
    ILogger<AuditReviewRecorder> logger)
{
    internal const string WriteFailureCode = "security_audit_review_denial_write_failed";
    internal const string ViewWriteFailureCode = "security_audit_review_view_write_failed";
    private const string ViewedIdempotencyPart = ":viewed";

    // Only event lists show audit records; the catalog holds no records and exports are their own operation.
    private static readonly HashSet<string> ViewedOperations =
    [
        SecurityAuditApiOperations.ListSecurityEvents,
        SecurityAuditApiOperations.ListPlatformSecurityEvents
    ];

    internal Task RecordDeniedAsync(HttpContext context) =>
        OperationId(context) is { } operationId
            ? RecordAsync(operationId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, string.Empty, WriteFailureCode, context.RequestAborted)
            : Task.CompletedTask;

    internal Task RecordViewedAsync(HttpContext context) =>
        OperationId(context) is { } operationId && ViewedOperations.Contains(operationId)
            ? RecordAsync(operationId, SecurityAuditOutcome.Succeeded, null, ViewedIdempotencyPart, ViewWriteFailureCode, context.RequestAborted)
            : Task.CompletedTask;

    private static string? OperationId(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<ModuleEndpointOpenApiConvention>()?.OperationId is { Length: > 0 } operationId
            ? operationId
            : null;

    // One record per actor, operation and outcome within the dedupe window, so refreshes and paging do not flood the audit.
    private async Task RecordAsync(string operationId, string outcome, string? reason, string idempotencyPart, string failureCode, CancellationToken requestAborted)
    {
        SecurityAuditOptions auditOptions = options.Value;
        TimeSpan dedupeWindow = auditOptions.DeniedReviewDedupeWindow > TimeSpan.Zero
            ? auditOptions.DeniedReviewDedupeWindow
            : SecurityAuditOptions.DefaultDeniedReviewDedupeWindow;
        TimeSpan writeTimeout = auditOptions.DeniedReviewWriteTimeout > TimeSpan.Zero
            ? auditOptions.DeniedReviewWriteTimeout
            : SecurityAuditOptions.DefaultDeniedReviewWriteTimeout;
        DateTimeOffset now = timeProvider.GetUtcNow();
        long window = now.UtcTicks / dedupeWindow.Ticks;
        string actor = string.IsNullOrWhiteSpace(requestContext.UserId) ? SecurityAuditActorKind.Anonymous : requestContext.UserId;
        string idempotencyKey = $"{actor}:{operationId}:{window}{idempotencyPart}{SecurityAuditEventCatalog.AuditReviewIdempotencySuffix}";
        string correlationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey;

        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        timeoutSource.CancelAfter(writeTimeout);

        try
        {
            _ = await writer.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.AuditReviewEvent,
                EventCategory = SecurityAuditEventCatalog.ReviewCategory,
                Outcome = outcome,
                ActorId = string.IsNullOrWhiteSpace(requestContext.UserId) ? null : requestContext.UserId,
                SubjectId = string.IsNullOrWhiteSpace(requestContext.UserId) ? null : requestContext.UserId,
                TenantId = string.IsNullOrWhiteSpace(requestContext.TenantId) ? SecurityAuditScopeCatalog.PlatformScopeId : requestContext.TenantId,
                Resource = SecurityAuditEventCatalog.EndpointResourcePrefix + operationId,
                Operation = operationId,
                OccurredAt = now.UtcDateTime,
                CorrelationId = correlationId,
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = idempotencyKey
            }, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // The review result stands when the audit store is unavailable; the writer emits the redacted host error.
        }
        catch (ArgumentException exception)
        {
            LogWriteFailure(failureCode, operationId, exception);
        }
        catch (OperationCanceledException exception)
        {
            LogWriteFailure(failureCode, operationId, exception);
        }
    }

    private void LogWriteFailure(string failureCode, string operationId, Exception exception)
    {
        logger.LogError(
            "Security audit review could not be recorded. Code={Code} Operation={Operation} Cause={Cause}",
            failureCode,
            operationId,
            exception.GetType().FullName);
    }
}
