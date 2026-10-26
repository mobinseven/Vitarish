using System.Globalization;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
        _ = group.MapModuleGet(FilesApiRoutes.UploadPolicy, GetUploadPolicy, "FilesGetUploadPolicy", typeof(FileUploadPolicyResponse));
        _ = group.MapModulePost(FilesApiRoutes.Upload, UploadFileAsync, "FilesUploadFile", typeof(FileUploadResponse), profile: ModuleEndpointProfile.MultipartUpload)
            .Accepts<FileUploadForm>(ModuleEndpointOpenApiConventions.MultipartFormData)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            // Kestrel and the form reader stop a body above the multipart ceiling instead of their smaller defaults.
            .WithMetadata(new RequestSizeLimitAttribute(D.FileTransferPolicy.MaximumMultipartRequestBytes))
            .WithFormOptions(multipartBodyLengthLimit: D.FileTransferPolicy.MaximumMultipartRequestBytes)
            .DisableAntiforgery();
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

    private static IResult GetUploadPolicy() => Results.Ok(new FileUploadPolicyResponse(
        D.FileTransferPolicy.MaximumContentBytes,
        [.. D.FileTransferPolicy.UploadFormats.Select(format => new FileUploadFormatResponse(format.MediaType, format.Extensions))]));

    private static async Task<IResult> UploadFileAsync(
        HttpRequest httpRequest,
        IFilesService filesService,
        IFileTransferAudit transferAudit,
        IFileTransferAuthorization transferAuthorization,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.HasFormContentType)
        {
            await RecordUploadRejectionAsync(transferAudit, null, null).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Multipart form content is required."));
        }

        if (httpRequest.ContentLength is > D.FileTransferPolicy.MaximumMultipartRequestBytes)
        {
            return await RejectTooLargeAsync(transferAudit, null, null).ConfigureAwait(false);
        }

        IFormCollection form;
        try
        {
            form = await httpRequest.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
        {
            // A body without a declared length is stopped by the request or multipart limit while it is read.
            return await RejectTooLargeAsync(transferAudit, null, null).ConfigureAwait(false);
        }

        IFormFile? content = form.Files.GetFile(FileUploadRequest.ContentFieldName);
        if (content is null || content.Length <= 0)
        {
            await RecordUploadRejectionAsync(transferAudit, content?.FileName, content?.Length).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "File content is required."));
        }

        if (content.Length > D.FileTransferPolicy.MaximumContentBytes)
        {
            return await RejectTooLargeAsync(transferAudit, content.FileName, content.Length).ConfigureAwait(false);
        }

        FileUploadRequest uploadRequest = ReadUploadRequest(form, content);
        if (string.IsNullOrWhiteSpace(uploadRequest.Checksum) ||
            !D.FileTransferPolicy.TryNormalizeContentType(content.ContentType, out string actualContentType) ||
            !D.FileTransferPolicy.TryNormalizeContentType(uploadRequest.ContentType, out string declaredContentType) ||
            !string.Equals(actualContentType, declaredContentType, StringComparison.Ordinal))
        {
            await RecordUploadRejectionAsync(transferAudit, uploadRequest.FileName, uploadRequest.Size).ConfigureAwait(false);
            return Results.BadRequest(new FilesError(FilesErrorCode.Validation, "Checksum and matching content type are required."));
        }

        uploadRequest = uploadRequest with { ContentType = declaredContentType };
        if (!D.FileTransferPolicy.TryResolveUploadFormat(declaredContentType, uploadRequest.FileName, out D.FileUploadFormat format) ||
            !await HasFormatSignatureAsync(content, format, cancellationToken).ConfigureAwait(false))
        {
            await transferAudit.RecordImportAsync(null, uploadRequest.FileName, uploadRequest.Size, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ContentTypeNotAllowed, CancellationToken.None).ConfigureAwait(false);
            return Results.Json(
                new FilesError(FilesErrorCode.UnsupportedContentType, "This file type is not accepted.", StatusCodes.Status415UnsupportedMediaType),
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (!await transferAuthorization.CanUploadAsync(cancellationToken).ConfigureAwait(false))
        {
            await transferAudit.RecordImportAsync(null, uploadRequest.FileName, uploadRequest.Size, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, CancellationToken.None).ConfigureAwait(false);
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

    private static async Task<IResult> RejectTooLargeAsync(IFileTransferAudit transferAudit, string? fileName, long? fileSize)
    {
        await transferAudit.RecordImportAsync(null, fileName, fileSize, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ContentTooLarge, CancellationToken.None).ConfigureAwait(false);
        return Results.Json(
            new FilesError(FilesErrorCode.ContentTooLarge, "File content exceeds the maximum supported size.", StatusCodes.Status413PayloadTooLarge),
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    private static async Task<bool> HasFormatSignatureAsync(IFormFile content, D.FileUploadFormat format, CancellationToken cancellationToken)
    {
        byte[] header = new byte[D.FileTransferPolicy.SignatureProbeBytes];
        await using Stream probe = content.OpenReadStream();
        int read = await probe.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        return D.FileTransferPolicy.MatchesSignature(format, header.AsSpan(0, read));
    }

    private static Task RecordUploadRejectionAsync(IFileTransferAudit transferAudit, string? fileName, long? fileSize) =>
        transferAudit.RecordImportAsync(null, fileName, fileSize, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);

    private static async Task<IResult> DownloadFileAsync(Guid fileId, HttpContext context, IFileMetadataAccess metadataAccess, IFileIntegrityService integrityService, IFileTransferAudit transferAudit, IServiceProvider services)
    {
        D.FileInformationReadDto? fileInfo = await metadataAccess.AuthorizeDownloadAsync(fileId, context.RequestAborted).ConfigureAwait(false);
        if (fileInfo is null || string.IsNullOrWhiteSpace(fileInfo.Checksum))
        {
            await transferAudit.RecordExportAsync(fileId, fileInfo?.FileName, fileInfo?.Metadata.Size, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.IntegrityValidationFailed, CancellationToken.None).ConfigureAwait(false);
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
            await transferAudit.RecordExportAsync(fileId, fileInfo.FileName, fileInfo.Metadata.Size, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (!verification.IsVerified || string.IsNullOrWhiteSpace(verification.ObjectKey))
        {
            await transferAudit.RecordExportAsync(fileId, fileInfo.FileName, verification.ExpectedLength, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.IntegrityValidationFailed, CancellationToken.None).ConfigureAwait(false);
            return Results.NotFound(new FilesError(FilesErrorCode.NotFound, "File content was not found."));
        }

        IObjectStorageService storageService = services.GetRequiredService<IObjectStorageService>();
        context.Response.ContentType = verification.ExpectedContentType ?? fileInfo.ContentType ?? "application/octet-stream";
        context.Response.ContentLength = verification.ExpectedLength;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            await transferAudit.RecordExportAsync(fileId, fileInfo.FileName, verification.ExpectedLength, SecurityAuditOutcome.Succeeded, null, context.RequestAborted).ConfigureAwait(false);
            return Results.Empty;
        }

        try
        {
            await storageService.StreamToAsync(verification.ObjectKey, context.Response.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
            await transferAudit.RecordExportAsync(fileId, fileInfo.FileName, verification.ExpectedLength, SecurityAuditOutcome.Succeeded, null, context.RequestAborted).ConfigureAwait(false);
            return Results.Empty;
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await transferAudit.RecordExportAsync(fileId, fileInfo.FileName, verification.ExpectedLength, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
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
        Tag = dto.Tag,
        FileName = D.FileTransferPolicy.NormalizeFileName(dto.FileName)
    };

    private static D.FileInformationReadDto ToDomain(FileUploadRequest request) => new()
    {
        AllowedOrigins = "*",
        ContentType = request.ContentType,
        Thumbnail = request.Thumbnail,
        Checksum = request.Checksum,
        Tag = request.Tag,
        FileName = request.FileName,
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
