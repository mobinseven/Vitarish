using System.Security.Cryptography;
using System.Threading.Channels;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public class FilesService(
    IFileInformationService fileInfoService,
    IObjectStorageService storage,
    IRequestContext requestContext) : IFilesService
{
    public async Task<FileInformationReadDto?> SaveFile(
        ChannelReader<byte[]> data,
        FileInformationReadDto givenInformation,
        CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(givenInformation.Checksum))
        {
            throw new InvalidDataException("Checksum is required");
        }

        IReadOnlyList<FileInformationReadDto> sameChecksum = await fileInfoService.GetListAsync(new FileInformationDescriptorDto { Checksum = givenInformation.Checksum }, cancellationToken).GetValueOrDefaultAsync() ?? [];

        FileInformationReadDto? existing = sameChecksum?.FirstOrDefault();
        if (existing is not null)
        {
            return existing;
        }

        ArgumentNullException.ThrowIfNull(data);


        if (string.IsNullOrWhiteSpace(givenInformation.ContentType))
        {
            throw new InvalidDataException("Content type should not be null");
        }

        string tempServerPath = Path.Combine(Path.GetTempPath(), $".tmp-{Guid.NewGuid():N}.part");

        using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using (FileStream fs = new(
            tempServerPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous))
        {
            await foreach (byte[]? chunk in data.ReadAllAsync(cancellationToken))
            {
                if (chunk is { Length: > 0 })
                {
                    await fs.WriteAsync(chunk, cancellationToken);
                    sha.AppendData(chunk);
                }
            }
            await fs.FlushAsync(cancellationToken);
        }

        string computedChecksum = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();

        if (!string.Equals(givenInformation.Checksum, computedChecksum, StringComparison.OrdinalIgnoreCase))
        {
            SafeDelete(tempServerPath);
            throw new InvalidOperationException("Checksum mismatch");
        }


        string extension = ContentTypeExtensions.SafeGetExtension(givenInformation.ContentType);
        FileInformationWriteDto info = new()
        {
            Owner = requestContext.TenantId,
            Checksum = computedChecksum,
            Thumbnail = givenInformation.Thumbnail,
            ContentType = givenInformation.ContentType
        };
        long fileSize = new FileInfo(tempServerPath).Length;

        FileInformationReadDto saved = await fileInfoService.AddAsync(info, cancellationToken).EnsureValueAsync();

        bool ex = await storage.ExistsAsync(saved.Checksum, cancellationToken);
        if (!ex)
        {
            await using FileStream uploadStream = new(
                tempServerPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous);
            await storage.UploadAsync(saved.Checksum, givenInformation.ContentType, uploadStream, fileSize, cancellationToken);
        }

        SafeDelete(tempServerPath);
        return saved;
    }

    private static void SafeDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
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
