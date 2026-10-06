using Microsoft.EntityFrameworkCore;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class FileTransferLifecycle(
    FilesDbContext dbContext,
    IRequestContext requestContext,
    IFileMetadataAccess metadataAccess,
    IFileIntegrityService integrityService,
    IEnumerable<IFileResourceAuthorization> authorizers) : IFileTransferLifecycle
{
    public async Task<FileUploadPreparation> PrepareAsync(
        FileTransferPurpose purpose,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        string tenantId = RequireTenant();
        ValidateResource(resourceType, resourceId);
        await EnsureAuthorizedAsync(purpose, resourceType, resourceId, cancellationToken).ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;
        DateTime expiresAt = now.Add(FileTransferPolicy.UnclaimedUploadLifetime);
        FileTransferRecord transfer = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Purpose = purpose,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ExpiresAt = expiresAt
        };
        _ = await dbContext.FileTransferRecords.AddAsync(transfer, cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FileUploadPreparation(transfer.Id, FileTransferPolicy.For(purpose), new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }

    public async Task<FileReference> ClaimAsync(Guid fileId, Guid transferId, CancellationToken cancellationToken = default)
    {
        FileTransferRecord transfer = await GetAuthorizedTransferAsync(transferId, cancellationToken).ConfigureAwait(false);
        FileReferenceRecord? existing = await dbContext.FileReferenceRecords
            .SingleOrDefaultAsync(reference => reference.FileInformationId == fileId && reference.TransferId == transferId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return new FileReference(fileId, FileLifecycleState.Reserved, FileIntegrityState.Pending, existing.State);
        }
        if (transfer.State is not FileTransferState.Prepared)
        {
            throw new InvalidOperationException("File transfer has already been consumed.");
        }
        FileInformationReadDto file = await metadataAccess.AuthorizeDownloadAsync(fileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("File is not available to the current tenant.");
        FileIntegrityVerificationResult integrity = await integrityService.VerifyAsync(file, cancellationToken).ConfigureAwait(false);
        if (!integrity.IsVerified)
        {
            throw new InvalidOperationException("File content is not verified.");
        }

        existing = new FileReferenceRecord
        {
            Id = Guid.NewGuid(),
            FileInformationId = fileId,
            TransferId = transferId,
            TenantId = RequireTenant()
        };
        _ = await dbContext.FileReferenceRecords.AddAsync(existing, cancellationToken).ConfigureAwait(false);
        transfer.Consume();
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FileReference(fileId, FileLifecycleState.Reserved, integrity.State, existing.State);
    }

    public async Task<FileReference> ReleaseAsync(Guid fileId, Guid transferId, CancellationToken cancellationToken = default)
    {
        FileTransferRecord transfer = await GetAuthorizedTransferAsync(transferId, cancellationToken).ConfigureAwait(false);
        FileReferenceRecord reference = await dbContext.FileReferenceRecords
            .SingleOrDefaultAsync(candidate => candidate.FileInformationId == fileId && candidate.TransferId == transfer.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("File reference reservation was not found.");
        reference.Release();
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new FileReference(fileId, FileLifecycleState.Temporary, FileIntegrityState.Pending, reference.State);
    }

    private async Task<FileTransferRecord> GetAuthorizedTransferAsync(Guid transferId, CancellationToken cancellationToken)
    {
        FileTransferRecord transfer = await dbContext.FileTransferRecords.SingleOrDefaultAsync(candidate => candidate.Id == transferId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("File transfer was not found.");
        if (transfer.IsExpired(DateTime.UtcNow))
        {
            transfer.Expire();
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("File transfer has expired.");
        }
        await EnsureAuthorizedAsync(transfer.Purpose, transfer.ResourceType, transfer.ResourceId, cancellationToken).ConfigureAwait(false);
        return transfer;
    }

    private async Task EnsureAuthorizedAsync(FileTransferPurpose purpose, string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        foreach (IFileResourceAuthorization authorizer in authorizers)
        {
            if (await authorizer.CanTransferAsync(purpose, resourceType, resourceId, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }
        throw new UnauthorizedAccessException("The requested resource does not authorize file transfer.");
    }

    private string RequireTenant() => requestContext.TenantId ?? throw new InvalidOperationException("Tenant context is required.");

    private static void ValidateResource(string resourceType, string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
        {
            throw new InvalidDataException("Resource type and resource ID are required.");
        }
    }
}
