using Microsoft.AspNetCore.Http;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Modules.Base.Core.Endpoints;

public sealed class SecurityAuditEndpointFilter(
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IEndpointFilter
{
    private const string EventName = "security.endpoint.mutation";
    private const string SourceBoundary = "main.endpoint-filter";
    private const string ResourcePrefix = "endpoint/";
    private const string StartedKeySuffix = ":started";
    private const string CompletedKeySuffix = ":completed";

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
            return AuditUnavailableResult();
        }

        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
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
            return AuditUnavailableResult();
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
                    "endpoint_exception"), context.HttpContext.RequestAborted);
            }
            catch (SecurityAuditStoreException)
            {
                return AuditUnavailableResult();
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
                statusCode >= StatusCodes.Status400BadRequest ? $"http_{statusCode}" : null), context.HttpContext.RequestAborted);
        }
        catch (SecurityAuditStoreException)
        {
            return AuditUnavailableResult();
        }

        return result;
    }

    private SecurityAuditEvent CreateEvent(string operationId, string outcome, string idempotencyKey, string? reason) => new()
    {
        EventName = EventName,
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

    private static IResult AuditUnavailableResult() => Results.Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Security audit is unavailable.",
        detail: "The operation was not confirmed because its mandatory security audit could not be persisted.");
}
