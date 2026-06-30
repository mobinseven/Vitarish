using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.Core.Endpoints;

public sealed record ProtectedModuleEndpointMetadata(string Scheme);
public sealed record AnonymousModuleEndpointMetadata(string Justification);
public sealed record WebhookEndpointMetadata(string Provider, string VerificationStrategy);
public sealed record FileUploadEndpointMetadata(string FormFileFieldName);
public sealed record BinaryDownloadEndpointMetadata(string ContentType);
public sealed record StreamingEndpointMetadata(string ContentType);
public sealed record LongRunningCommandEndpointMetadata;
public sealed record OpenApiExclusionJustificationMetadata(string Justification);
public sealed record ModuleEndpointAntiforgeryMetadata;

public static class ModuleEndpointAntiforgeryExtensions
{
    public static IApplicationBuilder UseModuleEndpointAntiforgery(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            Endpoint? endpoint = context.GetEndpoint();
            ModuleEndpointAntiforgeryMetadata? metadata = endpoint?.Metadata.GetMetadata<ModuleEndpointAntiforgeryMetadata>();

            if (metadata is null || IsBearerRequest(context))
            {
                await next(context);
                return;
            }

            if (HttpMethods.IsPost(context.Request.Method) ||
                HttpMethods.IsPut(context.Request.Method) ||
                HttpMethods.IsPatch(context.Request.Method) ||
                HttpMethods.IsDelete(context.Request.Method))
            {
                IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException exception)
                {
                    ILoggerFactory loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    ILogger logger = loggerFactory.CreateLogger("Vitarish.Modules.Base.Core.Endpoints.ModuleEndpointAntiforgery");
                    logger.LogInformation(exception, "Module endpoint antiforgery validation failed for {Method} {Path}.", context.Request.Method, context.Request.Path);

                    ProblemDetails problem = new()
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Antiforgery validation failed.",
                        Detail = "The request could not be validated. Refresh the page and try again.",
                        Type = "https://vitarish.local/problems/antiforgery-validation-failed",
                        Instance = context.Request.Path
                    };
                    problem.Extensions["code"] = "AntiforgeryValidationFailed";

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }
            }

            await next(context);
        });
    }

    private static bool IsBearerRequest(HttpContext context) =>
        context.Request.Headers.Authorization.Any(value =>
            value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true);
}
