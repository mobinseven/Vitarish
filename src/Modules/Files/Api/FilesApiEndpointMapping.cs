using System.Globalization;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Requests;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Contracts.Routes;
using C = Vitarish.Files.Contracts.Metadata;
using D = Vitarish.Files.Domain.Models;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Files.Api;

public static class FilesApiEndpointMapping
{
    public static IEndpointRouteBuilder MapFilesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(FilesApiRoutes.Base);
        _ = group.MapModuleGet(FilesApiRoutes.ById, GetFileInformationAsync, "FilesGetFileInformation", typeof(C.FileInformationDto));
        _ = group.MapModulePost("/search", FindFilesAsync, "FilesFindFiles", typeof(C.FileInformationDto[]));
        _ = group.MapModulePost(string.Empty, CreateFileRecordAsync, "FilesCreateFileRecord", typeof(C.FileInformationDto));
        _ = group.MapModulePost(FilesApiRoutes.Upload, UploadFileAsync, "FilesUploadFile", typeof(FileUploadResponse), profile: ModuleEndpointProfile.MultipartUpload);
        _ = group.MapModuleGet(FilesApiRoutes.ContentById, DownloadFileAsync, "FilesDownloadFile", successStatusCode: StatusCodes.Status200OK, profile: ModuleEndpointProfile.BinaryDownload, contentType: ModuleEndpointOpenApiConventions.BinaryStream);
        _ = group.MapModuleHead(FilesApiRoutes.ContentById, DownloadFileAsync, "FilesDownloadFileHead", successStatusCode: StatusCodes.Status200OK, contentType: ModuleEndpointOpenApiConventions.BinaryStream);
        return endpoints;
    }

    private static async Task<IResult> GetFileInformationAsync(Guid fileId, IFileMetadataAccess access, CancellationToken cancellationToken)
    {
        D.FileInformationReadDto? file = await access.GetAsync(fileId, cancellationToken).ConfigureAwait(false);
        return file is null
            ? Results.NotFound(new FilesError(FilesErrorCode.NotFound, "File information was not found."))
            : Results.Ok(ToContract(file));
    }

    private static async Task<IResult> FindFilesAsync(ListFilesRequest request, IFileMetadataAccess access, CancellationToken cancellationToken)
    {
        IReadOnlyList<D.FileInformationReadDto> files = await access.ListAsync(ToDomain(request.Descriptor), cancellationToken).ConfigureAwait(false);
        return Results.Ok(files.Select(ToContract).ToList());
    }

    private static async Task<IResult> CreateFileRecordAsync(CreateFileRecordRequest request, IFileMetadataAccess access, CancellationToken cancellationToken)
    {
        if (request.File is null || string.IsNullOrWhiteSpace(request.File.ContentType))
        {
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Content type is required."));
        }

        D.FileInformationReadDto? file = await access.CreateAsync(ToDomain(request.File), cancellationToken).ConfigureAwait(false);
        return file is null
            ? Results.Conflict(new FilesError(FilesErrorCode.Conflict, "File record could not be created."))
            : Results.Ok(ToContract(file));
    }

    private static async Task<IResult> UploadFileAsync(
        HttpRequest httpRequest,
        IFilesService filesService,
        IFileTransferAudit transferAudit,
        IFileTransferAuthorization transferAuthorization,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.HasFormContentType)
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Multipart form content is required."));
        }

        if (httpRequest.ContentLength is > D.FileTransferPolicy.MaximumMultipartRequestBytes)
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return CreateMultipartRequestTooLargeResult();
        }

        IFormCollection form;
        try
        {
            form = await httpRequest.ReadFormAsync(CreateBoundedFormOptions(), cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return CreateMultipartRequestTooLargeResult();
        }

        IFormFile? content = form.Files.GetFile(FilesApiRoutes.UploadContentField);
        if (content is null || content.Length <= 0)
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "File content is required."));
        }

        if (content.Length > D.FileTransferPolicy.MaximumContentBytes)
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "File content exceeds the maximum supported size."));
        }

        FileUploadRequest uploadRequest = ReadUploadRequest(form, content);
        if (string.IsNullOrWhiteSpace(uploadRequest.Checksum) ||
            !D.FileTransferPolicy.TryNormalizeContentType(content.ContentType, out string actualContentType) ||
            !D.FileTransferPolicy.TryNormalizeContentType(uploadRequest.ContentType, out string declaredContentType) ||
            !string.Equals(actualContentType, declaredContentType, StringComparison.Ordinal))
        {
            await RecordUploadRejectionAsync(transferAudit).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Checksum and matching content type are required."));
        }

        uploadRequest = uploadRequest with { ContentType = declaredContentType };

        if (!await transferAuthorization.CanUploadAsync(cancellationToken).ConfigureAwait(false))
        {
            await transferAudit.RecordImportAsync(null, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, CancellationToken.None).ConfigureAwait(false);
            return Results.Forbid();
        }

        await using Stream input = content.OpenReadStream();
        using CancellationTokenSource pumpCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<byte[]> channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });

        Task pump = PumpStreamAsync(input, channel.Writer, pumpCancellation.Token);
        try
        {
            D.FileInformationReadDto? saved = await filesService.SaveFile(channel.Reader, ToDomain(uploadRequest), cancellationToken).ConfigureAwait(false);
            if (!pumpCancellation.IsCancellationRequested)
            {
                await pumpCancellation.CancelAsync().ConfigureAwait(false);
            }

            try
            {
                await pump.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (pumpCancellation.IsCancellationRequested)
            {
                // The Application service may return early when content already exists.
            }
            return saved is null
                ? Results.Conflict(new FilesError(FilesErrorCode.Conflict, "File could not be uploaded."))
                : Results.Ok(new FileUploadResponse(ToContract(saved)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (InvalidDataException ex)
        {
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new FilesError(FilesErrorCode.Conflict, ex.Message));
        }
        catch (Exception)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        finally
        {
            if (!pumpCancellation.IsCancellationRequested)
            {
                await pumpCancellation.CancelAsync().ConfigureAwait(false);
            }

            _ = channel.Writer.TryComplete();
            try
            {
                await pump.ConfigureAwait(false);
            }
            catch
            {
                // Suppress background pump exceptions to avoid masking the primary exception
            }

            while (channel.Reader.TryRead(out byte[]? unreadChunk))
            {
                CryptographicOperations.ZeroMemory(unreadChunk);
            }
        }
    }

    private static Task RecordUploadRejectionAsync(IFileTransferAudit transferAudit) =>
        transferAudit.RecordImportAsync(null, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);

    private static IResult CreateMultipartRequestTooLargeResult() =>
        Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Multipart request exceeds the maximum supported size."));

    private static FormOptions CreateBoundedFormOptions() => new()
    {
        MultipartBodyLengthLimit = D.FileTransferPolicy.MaximumMultipartRequestBytes,
        ValueLengthLimit = D.FileTransferPolicy.MaximumMultipartFieldValueLength,
        KeyLengthLimit = D.FileTransferPolicy.MaximumMultipartFieldNameLength,
        ValueCountLimit = D.FileTransferPolicy.MaximumMultipartValueCount
    };

    private static async Task<IResult> DownloadFileAsync(Guid fileId, HttpContext context, IFileMetadataAccess metadataAccess, IFileIntegrityService integrityService, IFileTransferAudit transferAudit, IServiceProvider services)
    {
        D.FileInformationReadDto? fileInfo = await metadataAccess.AuthorizeDownloadAsync(fileId, context.RequestAborted).ConfigureAwait(false);
        if (fileInfo is null || string.IsNullOrWhiteSpace(fileInfo.Checksum))
        {
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.IntegrityValidationFailed, CancellationToken.None).ConfigureAwait(false);
            return Results.NotFound(new FilesError(FilesErrorCode.NotFound, "File content was not found."));
        }

        D.FileIntegrityVerificationResult verification;
        try
        {
            verification = await integrityService.VerifyAsync(fileInfo, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (!verification.IsVerified || string.IsNullOrWhiteSpace(verification.ObjectKey))
        {
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.IntegrityValidationFailed, CancellationToken.None).ConfigureAwait(false);
            return Results.NotFound(new FilesError(FilesErrorCode.NotFound, "File content was not found."));
        }

        IObjectStorageService storageService = services.GetRequiredService<IObjectStorageService>();
        context.Response.ContentType = verification.ExpectedContentType ?? fileInfo.ContentType ?? "application/octet-stream";
        context.Response.ContentLength = verification.ExpectedLength;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Succeeded, null, context.RequestAborted).ConfigureAwait(false);
            return Results.Empty;
        }

        try
        {
            await storageService.StreamToAsync(verification.ObjectKey, context.Response.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Succeeded, null, context.RequestAborted).ConfigureAwait(false);
            return Results.Empty;
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await transferAudit.RecordExportAsync(fileId, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task PumpStreamAsync(Stream input, ChannelWriter<byte[]> writer, CancellationToken cancellationToken)
    {
        try
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
            while (true)
            {
                int read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                byte[] chunk = buffer[..read].ToArray();
                await writer.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
            }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
    }

    private static FileUploadRequest ReadUploadRequest(IFormCollection form, IFormFile content) => new()
    {
        FileName = GetFormValue(form, nameof(FileUploadRequest.FileName)) ?? content.FileName,
        ContentType = GetFormValue(form, nameof(FileUploadRequest.ContentType)) ?? content.ContentType,
        Size = long.TryParse(GetFormValue(form, nameof(FileUploadRequest.Size)), NumberStyles.Integer, CultureInfo.InvariantCulture, out long size) ? size : content.Length,
        Checksum = GetFormValue(form, nameof(FileUploadRequest.Checksum)) ?? string.Empty,
        Thumbnail = GetFormValue(form, nameof(FileUploadRequest.Thumbnail)),
        Tag = GetFormValue(form, nameof(FileUploadRequest.Tag))
    };

    private static string? GetFormValue(IFormCollection form, string key) => form.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues value) ? value.ToString() : null;

    private static D.FileInformationDescriptorDto ToDomain(C.FileInformationDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Skip = dto.Skip,
        Take = dto.Take,
        Search = dto.Search,
        Url = dto.Url,
        Name = dto.Name,
        Owner = dto.Owner,
        Checksum = dto.Checksum,
        Tag = dto.Tag
    };

    private static D.FileInformationWriteDto ToDomain(C.FileInformationWriteRequest dto) => new()
    {
        AllowedOrigins = dto.AllowedOrigins,
        ContentType = dto.ContentType,
        Thumbnail = dto.Thumbnail,
        Checksum = dto.Checksum,
        Url = dto.Url,
        Owner = dto.Owner,
        Tag = dto.Tag
    };

    private static D.FileInformationReadDto ToDomain(FileUploadRequest request) => new()
    {
        AllowedOrigins = "*",
        ContentType = request.ContentType,
        Thumbnail = request.Thumbnail,
        Checksum = request.Checksum,
        Tag = request.Tag,
        Metadata = new D.FileMetadata { Size = request.Size }
    };

    private static C.FileInformationDto ToContract(D.FileInformationReadDto dto) => new()
    {
        Id = dto.Id,
        AllowedOrigins = dto.AllowedOrigins,
        ContentType = dto.ContentType,
        Thumbnail = dto.Thumbnail,
        Checksum = dto.Checksum,
        Url = dto.Url,
        Owner = dto.Owner,
        Tag = dto.Tag,
        Metadata = ToContract(dto.Metadata),
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy
    };

    private static C.FileMetadataDto ToContract(D.FileMetadata metadata) => new()
    {
        Size = metadata.Size,
        Extension = metadata.Extension,
        Dimensions = metadata.Dimensions is null ? null : new C.FileDimensionsDto { Width = metadata.Dimensions.Width, Height = metadata.Dimensions.Height }
    };
}
