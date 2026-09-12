using Microsoft.AspNetCore.Http;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Modules.Base.Core.Endpoints;

public sealed class SecurityAuditEndpointFilter(
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IEndpointFilter
{
    private const string EventName = SecurityAuditEventCatalog.EndpointMutationEvent;
    private const string SourceBoundary = SecurityAuditEventCatalog.EndpointFilterSourceBoundary;
    private const string ResourcePrefix = SecurityAuditEventCatalog.EndpointResourcePrefix;
    private const string StartedKeySuffix = SecurityAuditEventCatalog.StartedKeySuffix;
    private const string CompletedKeySuffix = SecurityAuditEventCatalog.CompletedKeySuffix;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<SecurityAuditHandledByApplicationMetadata>() is not null)
        {
            return await next(context);
        }

        ModuleEndpointOpenApiConvention? convention = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ModuleEndpointOpenApiConvention>();
        if (convention is null)
        {
            return await next(context);
        }

        if (convention.Profile is ModuleEndpointProfile.AnonymousJson
            && context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<SecurityAuditRequiredMetadata>() is null)
        {
            return await next(context);
        }

        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return await next(context);
        }

        string? operationKey = context.HttpContext.Request.Headers[RequestIdentityConstants.RequestIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status428PreconditionRequired,
                title: "A stable request identifier is required.",
                detail: $"Retry with the {RequestIdentityConstants.RequestIdHeader} header so a retried mutation can be correlated and deduplicated.");
        }

        string startedKey = $"{operationKey}:{convention.OperationId}{StartedKeySuffix}";
        string completedKey = $"{operationKey}:{convention.OperationId}{CompletedKeySuffix}";

        try
        {
            SecurityAuditWriteResult started = await securityAuditWriter.WriteAsync(CreateEvent(
                convention.OperationId,
                SecurityAuditOutcome.Started,
                startedKey,
                null), context.HttpContext.RequestAborted);
            if (started is SecurityAuditWriteResult.AlreadyRecorded)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Security operation outcome is not confirmed.",
                    detail: "Retry with the same request identifier after the previous outcome has been reviewed.");
            }
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the initiating operation result when the audit
            // store is unavailable; the writer emits the redacted host error.
        }

        object? result;
        try
        {
            result = await next(context);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            try
            {
                _ = await securityAuditWriter.WriteAsync(CreateEvent(
                    convention.OperationId,
                    SecurityAuditOutcome.Failed,
                    completedKey,
                    SecurityAuditReasonCatalog.EndpointException), context.HttpContext.RequestAborted);
            }
            catch (SecurityAuditStoreException)
            {
                // Preserve the real operation exception under AU-8.
            }

            throw;
        }

        int statusCode = result is IStatusCodeHttpResult statusResult && statusResult.StatusCode is int status
            ? status
            : StatusCodes.Status200OK;
        string outcome = statusCode >= StatusCodes.Status500InternalServerError
            ? SecurityAuditOutcome.Failed
            : statusCode >= StatusCodes.Status400BadRequest
                ? SecurityAuditOutcome.Denied
                : SecurityAuditOutcome.Succeeded;

        try
        {
            _ = await securityAuditWriter.WriteAsync(CreateEvent(
                convention.OperationId,
                outcome,
                completedKey,
                 statusCode >= StatusCodes.Status400BadRequest ? SecurityAuditReasonCatalog.HttpStatus(statusCode) : null), context.HttpContext.RequestAborted);
        }
        catch (SecurityAuditStoreException)
        {
            // Preserve the real operation result under AU-8.
        }

        return result;
    }

    private SecurityAuditEvent CreateEvent(string operationId, string outcome, string idempotencyKey, string? reason) => new()
    {
        EventName = EventName,
        EventCategory = SecurityAuditEventCatalog.FunctionCategory,
        Outcome = outcome,
        ActorId = requestContext.UserId,
        SubjectId = requestContext.UserId,
        TenantId = requestContext.TenantId!,
        Resource = ResourcePrefix + operationId,
        Operation = operationId,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
        SourceBoundary = SourceBoundary,
        RedactedReason = reason,
        IdempotencyKey = idempotencyKey
    };

}
