
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using SkiaSharp;
using System.Security.Cryptography;
using System.Threading.Channels;
using Vorn.Entities.Models;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;
using Vorn.Files.Host.Infrastructure.Interfaces;

namespace Vorn.Files.Host.Infrastructure.Services;

public class FilesService(
    VornOptions options,
    IFileInformationService fileInfoService,
    ILogger<FilesService> logger,
    IObjectStorageService storage,
    IThumbnailService thumbnailService,
    IWebpConversionService webpConversionService,
    IMultiTenantContextAccessor<TenantInfo> tenantAccessor) : IFilesService
{
    /// <summary>
    /// Saves a file from a chunked ChannelReader stream in a single pass (read → write → hash),
    /// validates checksum, de-duplicates by checksum, then atomically publishes the file
    /// and persists metadata. Designed for minimal complexity and high stability.
    /// </summary>
    public async Task<FileInformationDto?> SaveFile(
        ChannelReader<byte[]> data,
        FileInformationDto givenInformation,
        CancellationToken cancellationToken = default)
    {

        if(string.IsNullOrWhiteSpace(givenInformation.Checksum))
        {
            throw new InvalidDataException("Checksum is required");
        }

        IMultiTenantContext<TenantInfo>? tenantContext = tenantAccessor.MultiTenantContext;
        TenantInfo? resolvedTenant = tenantContext?.TenantInfo;

        if(resolvedTenant is null || string.IsNullOrWhiteSpace(resolvedTenant.Id))
        {
            throw new InvalidOperationException("Tenant context could not be resolved for the current request.");
        }

        if(!string.IsNullOrWhiteSpace(givenInformation.Owner) &&
            !string.Equals(givenInformation.Owner, resolvedTenant.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The provided file owner does not match the resolved tenant context.");
        }

        givenInformation.Owner = resolvedTenant.Id;

        using IDisposable tenantScope = tenantAccessor.UseTenant(resolvedTenant.Id);

        EntityResult<IReadOnlyList<FileInformationDto>> sameChecksumResult = await fileInfoService.GetListAsync(new FileInformationDescriptorDto { Checksum = givenInformation.Checksum }, CancellationToken.None);
        IReadOnlyList<FileInformationDto>? sameChecksum = sameChecksumResult.Value;
        FileInformationDto? existingForTenant = sameChecksum?.FirstOrDefault(info => string.Equals(info.Owner, resolvedTenant.Id, StringComparison.Ordinal));
        if(existingForTenant is not null)
        {
            logger.LogInformation("Duplicate detected. Reusing existing file for tenant {TenantId} (Id={Id}).", resolvedTenant.Id, existingForTenant.Id);
            return existingForTenant;
        }

        FileInformationDto? existing = sameChecksum?.FirstOrDefault();
        if(existing is not null)
        {
            logger.LogInformation("Duplicate detected for new owner. Cloning existing file (SourceId={SourceId}).", existing.Id);

            FileMetadata incomingMetadata = givenInformation.Metadata;
            FileMetadata existingMetadata = existing.Metadata;
            FileMetadata clonedMetadata = HasMetadata(incomingMetadata)
                ? MergeMetadata(incomingMetadata, existingMetadata)
                : existingMetadata with { };

            FileInformationDto cloned = new()
            {
                AllowedOrigins = string.IsNullOrWhiteSpace(givenInformation.AllowedOrigins)
                    ? existing.AllowedOrigins
                    : givenInformation.AllowedOrigins,
                Checksum = existing.Checksum,
                Owner = givenInformation.Owner,
                Url = existing.Url,
                Thumbnail = givenInformation.Thumbnail ?? existing.Thumbnail,
                ContentType = string.IsNullOrWhiteSpace(givenInformation.ContentType)
                    ? existing.ContentType
                    : givenInformation.ContentType,
                Metadata = clonedMetadata,
                Tag = givenInformation.Tag
            };

            _ = await fileInfoService.AddAsync(cloned, CancellationToken.None).ConfigureAwait(false);
            return cloned;
        }

        ArgumentNullException.ThrowIfNull(data);

        FileMetadata requestMetadata = givenInformation.Metadata;

        if(string.IsNullOrWhiteSpace(givenInformation.ContentType))
        {
            throw new InvalidDataException("Content type should not be null");
        }

        string contentType = givenInformation.ContentType;

        string tempServerPath = Path.Combine(Path.GetTempPath(), $".tmp-{Guid.NewGuid():N}.part");

        try
        {
            logger.LogInformation("SaveFile started. ContentType={ContentType}, Owner={Owner}", contentType, givenInformation.Owner);
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using(FileStream fs = new(
                tempServerPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous))
            {
                await foreach(byte[]? chunk in data.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    if(chunk is { Length: > 0 })
                    {
                        await fs.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
                        sha.AppendData(chunk);
                    }
                }
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            string computedChecksum = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
            logger.LogDebug("Computed checksum: {Checksum}", computedChecksum);

            if(!string.Equals(givenInformation.Checksum, computedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                SafeDelete(tempServerPath);
                throw new InvalidOperationException("Checksum mismatch");
            }


            string extension = ContentTypeExtensions.SafeGetExtension(contentType);
            FileInformationDto info = new()
            {
                Id = Guid.NewGuid()
            };
            string finalFileName = $"{options.Files.Prefix}-{info.Id}.{extension}";
            string objectKey = options.Files.BuildObjectKey(finalFileName);
            bool isImage = contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase);

            info.AllowedOrigins = givenInformation.AllowedOrigins;
            info.Checksum = computedChecksum;
            info.Owner = givenInformation.Owner;
            info.Url = BuildPublicUrl(options.Files.BaseUrl, options.Files.UploadPath, finalFileName);
            info.Thumbnail = givenInformation.Thumbnail ?? (info.Url + "?thumb=1");
            info.ContentType = contentType;
            long fileSize = new FileInfo(tempServerPath).Length;
            info.Metadata = BuildMetadata(requestMetadata, fileSize, extension, tempServerPath, isImage);

            await using(FileStream uploadStream = new(
                tempServerPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous))
            {
                await storage.UploadAsync(objectKey, contentType, uploadStream, fileSize, cancellationToken).ConfigureAwait(false);
            }

            _ = await fileInfoService.AddAsync(info, CancellationToken.None).ConfigureAwait(false);

            if(givenInformation.Thumbnail is null)
            {
                thumbnailService.Enqueue(objectKey, contentType);
            }

            if(isImage)
            {
                webpConversionService.Enqueue(objectKey, contentType);
            }

            logger.LogInformation("SaveFile finished. FileId={Id}, Size={Size} bytes, Url={Url}", info.Id, fileSize, info.Url);
            SafeDelete(tempServerPath);
            return info;
        }
        catch(OperationCanceledException)
        {
            logger.LogWarning("SaveFile canceled.");
            SafeDelete(tempServerPath);
            throw;
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "SaveFile failed. Cleaning up temp file if present.");
            SafeDelete(tempServerPath);
            throw;
        }
    }

    private static string BuildPublicUrl(string baseUrl, string uploadPath, string fileName)
    {
        baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
        string upload = (uploadPath ?? string.Empty).Trim('/');
        return string.IsNullOrEmpty(upload)
            ? $"{baseUrl}/{fileName}"
            : $"{baseUrl}/{upload}/{fileName}";
    }

    private static FileMetadata BuildMetadata(
        FileMetadata providedMetadata,
        long fileSize,
        string extension,
        string filePath,
        bool isImage)
    {
        (int Width, int Height)? dimensions = isImage
            ? TryGetImageDimensions(filePath)
            : null;

        FileMetadata metadata = providedMetadata with { };

        FileDimensions? resolvedDimensions = metadata.Dimensions ?? (dimensions is { Width: var width, Height: var height }
            ? new FileDimensions { Width = width, Height = height }
            : null);

        return metadata with
        {
            Size = fileSize,
            Extension = extension,
            Dimensions = resolvedDimensions
        };
    }

    private static bool HasMetadata(FileMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.Extension)
        || metadata.Size > 0
        || metadata.Dimensions is not null;
    }

    private static FileMetadata MergeMetadata(FileMetadata incoming, FileMetadata existing)
    {
        FileMetadata merged = incoming with { };

        if(string.IsNullOrWhiteSpace(merged.Extension))
        {
            merged = merged with { Extension = existing.Extension };
        }

        if(merged.Size <= 0)
        {
            merged = merged with { Size = existing.Size };
        }

        if(merged.Dimensions is null)
        {
            merged = merged with { Dimensions = existing.Dimensions };
        }

        return merged;
    }

    private static (int Width, int Height)? TryGetImageDimensions(string filePath)
    {
        try
        {
            using SKBitmap bitmap = SKBitmap.Decode(filePath);
            return (bitmap.Width, bitmap.Height);
        }
        catch
        {
            return null;
        }
    }

    private static void SafeDelete(string? path)
    {
        try
        {
            if(!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort cleanup only
        }
    }
}
