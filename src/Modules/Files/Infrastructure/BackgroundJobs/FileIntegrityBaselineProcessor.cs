using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Files.Infrastructure.BackgroundJobs;

/// <summary>
/// Resumptively enrolls one legacy file per tenant on each scheduled invocation.
/// </summary>
public sealed class FileIntegrityBaselineProcessor(TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:files:integrity-baseline";

    public Task ProcessAsync(CancellationToken cancellationToken = default) =>
        tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            static (provider, token) => provider.GetRequiredService<FileIntegrityBaselineTenantProcessor>().ProcessAsync(token),
            cancellationToken);
}

internal sealed class FileIntegrityBaselineTenantProcessor(
    FilesDbContext dbContext,
    IFileIntegrityService integrityService,
    IFileTransferAudit transferAudit,
    ILogger<FileIntegrityBaselineTenantProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        FileInformation? file = await dbContext.Files
            .Where(candidate => !dbContext.FileIntegrityRecords.Any(record =>
                record.FileInformationId == candidate.Id &&
                (record.State == FileIntegrityState.Verified || record.State == FileIntegrityState.Unavailable)))
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        bool requiresEnrollment = file is not null;

        if (file is null)
        {
            Guid? verifiedFileId = await dbContext.FileIntegrityRecords
                .Where(record => record.State == FileIntegrityState.Verified)
                .OrderBy(record => record.VerifiedAt)
                .Select(record => (Guid?)record.FileInformationId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (verifiedFileId is { } fileId)
            {
                file = await dbContext.Files
                    .FirstOrDefaultAsync(candidate => candidate.Id == fileId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (file is null)
        {
            return;
        }

        FileInformationReadDto legacyFile = new()
        {
            Id = file.Id,
            Owner = file.Owner,
            Checksum = file.Checksum,
            ContentType = file.ContentType,
            Metadata = new FileMetadata { Size = file.Size }
        };

        try
        {
            if (requiresEnrollment)
            {
                _ = await integrityService.BeginLegacyEnrollmentAsync(legacyFile, cancellationToken).ConfigureAwait(false);
            }

            FileIntegrityVerificationResult verification = await integrityService.VerifyAsync(legacyFile, cancellationToken).ConfigureAwait(false);
            await transferAudit.RecordIntegrityVerificationAsync(
                file.Id,
                verification.IsVerified ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Failed,
                verification.IsVerified ? null : SecurityAuditReasonCatalog.IntegrityValidationFailed,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Files integrity baseline could not process file {FileId}.", file.Id);
            await transferAudit.RecordIntegrityVerificationAsync(
                file.Id,
                SecurityAuditOutcome.Failed,
                SecurityAuditReasonCatalog.IntegrityValidationFailed,
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
