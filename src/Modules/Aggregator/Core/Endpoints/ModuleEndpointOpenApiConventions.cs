using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Vitarish.Modules.Aggregator.Core.Endpoints;

public enum ModuleEndpointProfile
{
    ProtectedJson,
    AnonymousWebhook,
    MultipartUpload,
    BinaryDownload,
    Streaming,
    LongRunningCommand
}

public sealed record ModuleEndpointOpenApiConvention(
    string OperationId,
    int SuccessStatusCode = StatusCodes.Status200OK,
    Type? SuccessType = null,
    ModuleEndpointProfile Profile = ModuleEndpointProfile.ProtectedJson,
    string? AnonymousJustification = null,
    string? ContentType = null,
    bool IncludeStandardErrors = true);

public static class ModuleEndpointOpenApiConventions
{
    public const string DefaultBearerScheme = "IdentityServerAccessToken";
    public const string MultipartFormData = "multipart/form-data";
    public const string JsonStream = "application/x-ndjson";
    public const string BinaryStream = "application/octet-stream";

    public static RouteHandlerBuilder ApplyModuleOpenApiConvention(this RouteHandlerBuilder builder, ModuleEndpointOpenApiConvention convention)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(convention.OperationId);

        builder.WithName(convention.OperationId);
        builder.WithOpenApi(operation =>
        {
            operation.OperationId = convention.OperationId;
            return operation;
        });
        builder.WithMetadata(convention);

        if (convention.SuccessType is null)
        {
            builder.Produces(convention.SuccessStatusCode, contentType: convention.ContentType);
        }
        else
        {
            builder.Produces(convention.SuccessStatusCode, convention.SuccessType, convention.ContentType);
        }

        if (convention.IncludeStandardErrors)
        {
            if (convention.Profile is not ModuleEndpointProfile.AnonymousWebhook)
            {
                builder.Produces(StatusCodes.Status401Unauthorized);
                builder.Produces(StatusCodes.Status403Forbidden);
                builder.WithMetadata(new ProtectedModuleEndpointMetadata(DefaultBearerScheme));
            }

            builder.ProducesProblem(StatusCodes.Status400BadRequest);
            builder.ProducesProblem(StatusCodes.Status404NotFound);
            builder.ProducesProblem(StatusCodes.Status409Conflict);
        }

        switch (convention.Profile)
        {
            case ModuleEndpointProfile.AnonymousWebhook:
                ArgumentException.ThrowIfNullOrWhiteSpace(convention.AnonymousJustification);
                builder.WithMetadata(new AnonymousModuleEndpointMetadata(convention.AnonymousJustification));
                break;
            case ModuleEndpointProfile.MultipartUpload:
                builder.Accepts<IFormFile>(MultipartFormData);
                builder.WithMetadata(new FileUploadEndpointMetadata("content"));
                break;
            case ModuleEndpointProfile.BinaryDownload:
                builder.WithMetadata(new BinaryDownloadEndpointMetadata(convention.ContentType ?? BinaryStream));
                break;
            case ModuleEndpointProfile.Streaming:
                builder.WithMetadata(new StreamingEndpointMetadata(convention.ContentType ?? JsonStream));
                break;
            case ModuleEndpointProfile.LongRunningCommand:
                builder.WithMetadata(new LongRunningCommandEndpointMetadata());
                break;
        }

        return builder;
    }

    public static IEndpointConventionBuilder ExcludeFromOpenApiWithJustification(this IEndpointConventionBuilder builder, string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        builder.WithMetadata(new OpenApiExclusionJustificationMetadata(justification));
        builder.ExcludeFromDescription();
        return builder;
    }
}
