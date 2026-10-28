using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

/// <summary>
/// Records an unauthorized attempt to read the security audit at the review API boundary.
/// The review query itself stays side-effect-free, and authorized reads are not recorded.
/// </summary>
internal sealed class AuditReviewDenialFilter(AuditReviewDenialRecorder recorder) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (UnauthorizedAccessException)
        {
            await recorder.RecordAsync(context.HttpContext).ConfigureAwait(false);
            return Results.Forbid();
        }
    }
}

internal sealed class AuditReviewDenialRecorder(
    ISecurityAuditWriter writer,
    IRequestContext requestContext,
    IOptions<SecurityAuditOptions> options,
    TimeProvider timeProvider,
    ILogger<AuditReviewDenialRecorder> logger)
{
    internal const string WriteFailureCode = "security_audit_review_denial_write_failed";

    internal Task RecordAsync(HttpContext context)
    {
        string? operationId = context.GetEndpoint()?
            .Metadata.GetMetadata<ModuleEndpointOpenApiConvention>()?.OperationId;
        return string.IsNullOrWhiteSpace(operationId)
            ? Task.CompletedTask
            : RecordAsync(operationId, context.RequestAborted);
    }

    private async Task RecordAsync(string operationId, CancellationToken requestAborted)
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
        string idempotencyKey = $"{actor}:{operationId}:{window}{SecurityAuditEventCatalog.AuditReviewIdempotencySuffix}";
        string correlationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey;

        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        timeoutSource.CancelAfter(writeTimeout);

        try
        {
            _ = await writer.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.AuditReviewEvent,
                EventCategory = SecurityAuditEventCatalog.ReviewCategory,
                Outcome = SecurityAuditOutcome.Denied,
                ActorId = string.IsNullOrWhiteSpace(requestContext.UserId) ? null : requestContext.UserId,
                SubjectId = string.IsNullOrWhiteSpace(requestContext.UserId) ? null : requestContext.UserId,
                TenantId = string.IsNullOrWhiteSpace(requestContext.TenantId) ? SecurityAuditScopeCatalog.PlatformScopeId : requestContext.TenantId,
                Resource = SecurityAuditEventCatalog.EndpointResourcePrefix + operationId,
                Operation = operationId,
                OccurredAt = now.UtcDateTime,
                CorrelationId = correlationId,
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                RedactedReason = SecurityAuditReasonCatalog.Unauthorized,
                IdempotencyKey = idempotencyKey
            }, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // The denial stands when the audit store is unavailable; the writer emits the redacted host error.
        }
        catch (ArgumentException exception)
        {
            LogWriteFailure(operationId, exception);
        }
        catch (OperationCanceledException exception)
        {
            LogWriteFailure(operationId, exception);
        }
    }

    private void LogWriteFailure(string operationId, Exception exception)
    {
        logger.LogError(
            "Security audit review denial could not be recorded. Code={Code} Operation={Operation} Cause={Cause}",
            WriteFailureCode,
            operationId,
            exception.GetType().FullName);
    }
}
