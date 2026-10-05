using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vitarish.Hosting.Infrastructure.Pipeline;

/// <summary>
/// Maps client-triggered request failures to their correct 4xx problem responses,
/// reports client cancellation without logging it as a server fault, and leaves
/// genuine server faults on the default 500 path so bugs are not masked as 4xx.
/// </summary>
public sealed class HostRequestExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<HostRequestExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException &&
            (httpContext.RequestAborted.IsCancellationRequested || cancellationToken.IsCancellationRequested))
        {
            logger.LogDebug("The request was cancelled by the client before a response could be produced.");
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }

            return true;
        }

        if (!TryResolveClientErrorStatus(exception, out int statusCode))
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = Title(statusCode),
            Detail = environment.IsDevelopment() ? exception.Message : null
        };

        try
        {
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });
        }
        catch (Exception writeException) when (writeException is OperationCanceledException or IOException)
        {
            // The client transport is already gone; there is nothing left to answer.
            return true;
        }
    }

    private static bool TryResolveClientErrorStatus(Exception exception, out int statusCode)
    {
        statusCode = exception switch
        {
            BadHttpRequestException { StatusCode: >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError } badRequest => badRequest.StatusCode,
            JsonException => StatusCodes.Status400BadRequest,
            InvalidDataException => StatusCodes.Status400BadRequest,
            _ => 0
        };
        return statusCode != 0;
    }

    private static string Title(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "The request is invalid.",
        StatusCodes.Status413RequestEntityTooLarge => "The request body is too large.",
        StatusCodes.Status415UnsupportedMediaType => "The request content type is not supported.",
        _ => "The request could not be processed."
    };
}
