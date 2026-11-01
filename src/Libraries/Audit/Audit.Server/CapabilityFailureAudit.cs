using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

public static class CapabilityFailureAuditExtensions
{
    /// <summary>
    /// Records a security failure for each product operation that ends in a server error. Register it outside
    /// the exception handler: it reads the final response status, after that response has completed, so it
    /// neither changes nor delays the response.
    /// </summary>
    public static IApplicationBuilder UseCapabilityFailureAudit(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.OnCompleted(static state =>
            {
                HttpContext completed = (HttpContext)state;
                return completed.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? completed.RequestServices.GetRequiredService<CapabilityFailureRecorder>().RecordAsync(completed)
                    : Task.CompletedTask;
            }, context);
            return next(context);
        });
}

internal sealed class CapabilityFailureRecorder(
    ISecurityAuditWriter writer,
    IRequestContext requestContext,
    IOptions<SecurityAuditOptions> options,
    TimeProvider timeProvider,
    ILogger<CapabilityFailureRecorder> logger)
{
    private const string WriteFailureCode = "security_audit_capability_failure_write_failed";

    internal async Task RecordAsync(HttpContext context)
    {
        // Endpoints without a product operation, such as readiness probes, are not product capabilities.
        if (context.GetEndpoint()?.Metadata.GetMetadata<ModuleEndpointOpenApiConvention>()?.OperationId is not { Length: > 0 } operationId)
            return;

        int status = context.Response.StatusCode;
        SecurityAuditOptions auditOptions = options.Value;
        TimeSpan dedupeWindow = auditOptions.DeniedReviewDedupeWindow > TimeSpan.Zero
            ? auditOptions.DeniedReviewDedupeWindow
            : SecurityAuditOptions.DefaultDeniedReviewDedupeWindow;
        TimeSpan writeTimeout = auditOptions.DeniedReviewWriteTimeout > TimeSpan.Zero
            ? auditOptions.DeniedReviewWriteTimeout
            : SecurityAuditOptions.DefaultDeniedReviewWriteTimeout;
        DateTimeOffset now = timeProvider.GetUtcNow();

        // One record per operation and status within the window, so an outage cannot flood the audit.
        string idempotencyKey =
            $"{SecurityAuditEventCatalog.SecurityFailureOperation}:{operationId}:{status}:{now.UtcTicks / dedupeWindow.Ticks}";
        bool signedIn = !string.IsNullOrWhiteSpace(requestContext.UserId);

        // The request is over, so its abort token no longer applies; only the timeout bounds this write.
        using CancellationTokenSource timeout = new(writeTimeout);
        try
        {
            _ = await writer.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
                EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
                Outcome = SecurityAuditOutcome.Failed,
                ActorId = signedIn ? requestContext.UserId : null,
                SubjectId = signedIn ? requestContext.UserId : null,
                TenantId = string.IsNullOrWhiteSpace(requestContext.TenantId) ? SecurityAuditScopeCatalog.PlatformScopeId : requestContext.TenantId,
                Resource = SecurityAuditEventCatalog.EndpointResourcePrefix + operationId,
                Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
                OccurredAt = now.UtcDateTime,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
                SourceBoundary = SecurityAuditEventCatalog.CapabilityFailureSourceBoundary,
                RedactedReason = status is StatusCodes.Status503ServiceUnavailable or StatusCodes.Status504GatewayTimeout
                    ? SecurityAuditReasonCatalog.DependencyUnavailable
                    : SecurityAuditReasonCatalog.EndpointException,
                IdempotencyKey = idempotencyKey
            }, timeout.Token).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // The writer has counted this refused write, and the recovered-outage summary covers it.
        }
        catch (Exception exception)
        {
            // A failure callback has no caller left to tell, so this log is the failure's only report.
            logger.LogError(
                "Security failure could not be recorded. Code={Code} Operation={Operation} Cause={Cause}",
                WriteFailureCode,
                operationId,
                exception.GetType().FullName);
        }
    }
}
