using Microsoft.AspNetCore.Http;

namespace Vitarish.Modules.Base.Core.Endpoints;

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

        _ = builder.WithName(convention.OperationId);
        _ = builder.WithOpenApi(operation =>
        {
            operation.OperationId = convention.OperationId;
            return operation;
        });
        _ = builder.WithMetadata(convention);

        if (convention.SuccessType is null)
        {
            _ = builder.Produces(convention.SuccessStatusCode, contentType: convention.ContentType);
        }
        else
        {
            _ = builder.Produces(convention.SuccessStatusCode, convention.SuccessType, convention.ContentType);
        }

        if (convention.IncludeStandardErrors)
        {
            if (convention.Profile is not ModuleEndpointProfile.AnonymousWebhook)
            {
                _ = builder.Produces(StatusCodes.Status401Unauthorized);
                _ = builder.Produces(StatusCodes.Status403Forbidden);
                _ = builder.WithMetadata(new ProtectedModuleEndpointMetadata(DefaultBearerScheme));
            }

            _ = builder.ProducesProblem(StatusCodes.Status400BadRequest);
            _ = builder.ProducesProblem(StatusCodes.Status404NotFound);
            _ = builder.ProducesProblem(StatusCodes.Status409Conflict);
        }

        switch (convention.Profile)
        {
            case ModuleEndpointProfile.AnonymousWebhook:
                ArgumentException.ThrowIfNullOrWhiteSpace(convention.AnonymousJustification);
                _ = builder.WithMetadata(new AnonymousModuleEndpointMetadata(convention.AnonymousJustification));
                break;
            case ModuleEndpointProfile.MultipartUpload:
                _ = builder.Accepts<IFormFile>(MultipartFormData);
                _ = builder.WithMetadata(new FileUploadEndpointMetadata("content"));
                break;
            case ModuleEndpointProfile.BinaryDownload:
                _ = builder.WithMetadata(new BinaryDownloadEndpointMetadata(convention.ContentType ?? BinaryStream));
                break;
            case ModuleEndpointProfile.Streaming:
                _ = builder.WithMetadata(new StreamingEndpointMetadata(convention.ContentType ?? JsonStream));
                break;
            case ModuleEndpointProfile.LongRunningCommand:
                _ = builder.WithMetadata(new LongRunningCommandEndpointMetadata());
                break;
        }

        return builder;
    }

    public static IEndpointConventionBuilder ExcludeFromOpenApiWithJustification(this IEndpointConventionBuilder builder, string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        _ = builder.WithMetadata(new OpenApiExclusionJustificationMetadata(justification));
        _ = builder.ExcludeFromDescription();
        return builder;
    }
}
