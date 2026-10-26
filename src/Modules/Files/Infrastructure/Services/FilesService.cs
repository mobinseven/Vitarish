using System.Security.Cryptography;
using System.Threading.Channels;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public class FilesService(
    IFileInformationService fileInfoService,
    IObjectStorageService storage,
    IFileIntegrityService integrityService,
    IFileTransferAudit transferAudit,
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

        if (!FileTransferPolicy.TryNormalizeContentType(givenInformation.ContentType, out string normalizedContentType) ||
            !ContentTypeExtensions.TryGetExtension(normalizedContentType, out _))
        {
            throw new InvalidDataException("Content type is not supported");
        }

        ArgumentNullException.ThrowIfNull(data);

        byte[] claimedChecksum;
        try
        {
            claimedChecksum = Convert.FromHexString(givenInformation.Checksum);
        }
        catch (FormatException)
        {
            throw new InvalidDataException("Checksum must be a SHA-256 hexadecimal value");
        }

        if (claimedChecksum.Length != SHA256.HashSizeInBytes)
        {
            CryptographicOperations.ZeroMemory(claimedChecksum);
            throw new InvalidDataException("Checksum must be a SHA-256 hexadecimal value");
        }

        string tempServerPath = Path.Combine(Path.GetTempPath(), $".tmp-{Guid.NewGuid():N}.part");
        try
        {
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long receivedBytes = 0;
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
                    if (chunk is not { Length: > 0 })
                    {
                        continue;
                    }

                    try
                    {
                        if (chunk.Length > FileTransferPolicy.MaximumContentBytes - receivedBytes)
                        {
                            throw new InvalidDataException("File content exceeds the maximum supported size");
                        }

                        receivedBytes += chunk.Length;
                        await fs.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
                        sha.AppendData(chunk);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(chunk);
                    }
                }
                await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            byte[] computedChecksumBytes = sha.GetHashAndReset();
            string computedChecksum;
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(claimedChecksum, computedChecksumBytes))
                {
                    throw new InvalidOperationException("Checksum mismatch");
                }

                computedChecksum = Convert.ToHexString(computedChecksumBytes).ToLowerInvariant();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(claimedChecksum);
                CryptographicOperations.ZeroMemory(computedChecksumBytes);
            }

            string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required");
            IReadOnlyList<FileInformationReadDto> sameChecksum = await fileInfoService.GetListAsync(
                new FileInformationDescriptorDto { Checksum = computedChecksum, Owner = tenantId },
                cancellationToken).GetValueOrDefaultAsync().ConfigureAwait(false) ?? [];
            FileInformationReadDto? existing = sameChecksum.FirstOrDefault();
            if (existing is not null)
            {
                FileIntegrityVerificationResult existingIntegrity = await integrityService.VerifyAsync(existing, cancellationToken).ConfigureAwait(false);
                if (existingIntegrity.IsVerified)
                {
                    await transferAudit.RecordImportAsync(existing.Id, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Succeeded, null, cancellationToken).ConfigureAwait(false);
                    return existing;
                }
            }

            FileInformationWriteDto info = new()
            {
                Owner = tenantId,
                Checksum = computedChecksum,
                Thumbnail = givenInformation.Thumbnail,
                ContentType = normalizedContentType,
                FileName = FileTransferPolicy.NormalizeFileName(givenInformation.FileName)
            };
            long fileSize = new FileInfo(tempServerPath).Length;
            FileInformationReadDto saved = await fileInfoService.AddAsync(info, cancellationToken).EnsureValueAsync();
            FileIntegrityVerificationResult enrollment = await integrityService.BeginEnrollmentAsync(
                saved,
                fileSize,
                normalizedContentType,
                cancellationToken).ConfigureAwait(false);
            string storageKey = enrollment.ObjectKey ?? throw new InvalidOperationException("File integrity enrollment did not create an object key.");
            if (!await storage.ExistsAsync(storageKey, cancellationToken).ConfigureAwait(false))
            {
                await using FileStream uploadStream = new(
                    tempServerPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous);
                await storage.UploadAsync(storageKey, normalizedContentType, uploadStream, fileSize, cancellationToken).ConfigureAwait(false);
            }

            FileIntegrityVerificationResult verification = await integrityService.VerifyAsync(saved, cancellationToken).ConfigureAwait(false);
            if (!verification.IsVerified)
            {
                throw new InvalidOperationException("Uploaded content could not be verified.");
            }

            await transferAudit.RecordImportAsync(saved.Id, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Succeeded, null, cancellationToken).ConfigureAwait(false);
            return saved;
        }
        catch (InvalidDataException)
        {
            await transferAudit.RecordImportAsync(null, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (InvalidOperationException)
        {
            await transferAudit.RecordImportAsync(null, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.IntegrityValidationFailed, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transferAudit.RecordImportAsync(null, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception)
        {
            await transferAudit.RecordImportAsync(null, givenInformation.FileName, givenInformation.Metadata.Size, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.EndpointException, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(claimedChecksum);
            DeleteTemporaryFile(tempServerPath);
        }
    }

    private static void DeleteTemporaryFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
