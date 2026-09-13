using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class FileIntegrityService(
    FilesDbContext dbContext,
    IObjectStorageService storage) : IFileIntegrityService
{
    public async Task<FileIntegrityVerificationResult> BeginEnrollmentAsync(
        FileInformationReadDto file,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        string tenantId = file.Owner ?? throw new InvalidOperationException("File owner is required.");
        string digest = file.Checksum ?? throw new InvalidOperationException("File checksum is required.");
        if (file.Id == Guid.Empty || contentLength < 0 || contentLength > FileTransferPolicy.MaximumContentBytes ||
            !FileTransferPolicy.TryNormalizeContentType(contentType, out string normalizedContentType) ||
            !ContentTypeExtensions.TryGetExtension(normalizedContentType, out _))
        {
            throw new InvalidOperationException("Verified file enrollment requires complete trusted metadata.");
        }

        return await CreateEnrollmentAsync(file, contentLength, normalizedContentType, FileStorageKey.For(tenantId, file.Id), cancellationToken).ConfigureAwait(false);
    }

    public async Task<FileIntegrityVerificationResult> BeginLegacyEnrollmentAsync(
        FileInformationReadDto file,
        CancellationToken cancellationToken = default)
    {
        string digest = file.Checksum ?? throw new InvalidOperationException("Legacy file checksum is required.");
        if (file.Id == Guid.Empty || file.Metadata.Size < 0 || file.Metadata.Size > FileTransferPolicy.MaximumContentBytes ||
            !FileTransferPolicy.TryNormalizeContentType(file.ContentType, out string contentType) ||
            !ContentTypeExtensions.TryGetExtension(contentType, out _))
        {
            throw new InvalidOperationException("Legacy file enrollment requires complete persisted metadata.");
        }

        return await CreateEnrollmentAsync(file, file.Metadata.Size, contentType, digest, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FileIntegrityVerificationResult> VerifyAsync(
        FileInformationReadDto file,
        CancellationToken cancellationToken = default)
    {
        if (file.Id == Guid.Empty || string.IsNullOrWhiteSpace(file.Owner) || string.IsNullOrWhiteSpace(file.Checksum))
        {
            return new FileIntegrityVerificationResult(FileIntegrityState.Unavailable, null, 0, null);
        }

        FileIntegrityRecord? record = await dbContext.FileIntegrityRecords
            .SingleOrDefaultAsync(candidate => candidate.FileInformationId == file.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null || record.State is FileIntegrityState.Unavailable)
        {
            return new FileIntegrityVerificationResult(FileIntegrityState.Unavailable, null, 0, null);
        }

        bool hasAllowedTransferMetadata = record.ContentLength >= 0 &&
                                          record.ContentLength <= FileTransferPolicy.MaximumContentBytes &&
                                          FileTransferPolicy.TryNormalizeContentType(record.ContentType, out string normalizedRecordContentType) &&
                                          ContentTypeExtensions.TryGetExtension(normalizedRecordContentType, out _) &&
                                          string.Equals(record.ContentType, normalizedRecordContentType, StringComparison.Ordinal);
        bool valid = hasAllowedTransferMetadata &&
                     string.Equals(record.TenantId, file.Owner, StringComparison.Ordinal) &&
                     ChecksumMatches(record.Digest, file.Checksum) &&
                     await ObjectMatchesAsync(record, cancellationToken).ConfigureAwait(false);
        if (!valid)
        {
            record.MarkUnavailable();
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ToResult(record);
        }

        if (record.State is FileIntegrityState.Pending)
        {
            record.MarkVerified(DateTime.UtcNow);
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return ToResult(record);
    }

    private async Task<bool> ObjectMatchesAsync(FileIntegrityRecord record, CancellationToken cancellationToken)
    {
        if (!await storage.ExistsAsync(record.ObjectKey, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        ObjectMetadata metadata = await storage.GetMetadataAsync(record.ObjectKey, cancellationToken).ConfigureAwait(false);
        if (metadata.Size != record.ContentLength ||
            !FileTransferPolicy.TryNormalizeContentType(metadata.ContentType, out string objectContentType) ||
            !string.Equals(objectContentType, record.ContentType, StringComparison.Ordinal))
        {
            return false;
        }

        using SHA256 hash = SHA256.Create();
        await using CryptoStream digestSink = new(Stream.Null, hash, CryptoStreamMode.Write, leaveOpen: false);
        await storage.StreamToAsync(record.ObjectKey, digestSink, cancellationToken: cancellationToken).ConfigureAwait(false);
        digestSink.FlushFinalBlock();
        return hash.Hash is { } actualDigest && ChecksumMatches(record.Digest, Convert.ToHexString(actualDigest));
    }

    private static bool ChecksumMatches(string left, string right)
    {
        try
        {
            byte[] leftBytes = Convert.FromHexString(left);
            byte[] rightBytes = Convert.FromHexString(right);
            try
            {
                return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(leftBytes);
                CryptographicOperations.ZeroMemory(rightBytes);
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static FileIntegrityVerificationResult ToResult(FileIntegrityRecord record) =>
        new(record.State, record.State is FileIntegrityState.Verified ? record.ObjectKey : null, record.ContentLength, record.ContentType);

    private async Task<FileIntegrityVerificationResult> CreateEnrollmentAsync(
        FileInformationReadDto file,
        long contentLength,
        string contentType,
        string objectKey,
        CancellationToken cancellationToken)
    {
        string tenantId = file.Owner ?? throw new InvalidOperationException("File owner is required.");
        string digest = file.Checksum ?? throw new InvalidOperationException("File checksum is required.");
        if (file.Id == Guid.Empty || contentLength < 0 || string.IsNullOrWhiteSpace(contentType))
        {
            throw new InvalidOperationException("File enrollment requires complete trusted metadata.");
        }

        FileIntegrityRecord? existing = await dbContext.FileIntegrityRecords
            .SingleOrDefaultAsync(candidate => candidate.FileInformationId == file.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ToResult(existing);
        }

        FileIntegrityRecord record = new()
        {
            FileInformationId = file.Id,
            TenantId = tenantId,
            ObjectKey = objectKey,
            Digest = digest,
            ContentType = contentType,
            ContentLength = contentLength
        };
        _ = await dbContext.FileIntegrityRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FileIntegrityVerificationResult(record.State, record.ObjectKey, record.ContentLength, record.ContentType);
    }
}
