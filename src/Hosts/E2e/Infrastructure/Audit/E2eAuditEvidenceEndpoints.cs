using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.E2e.Infrastructure.Audit;

internal static class E2eAuditEvidenceEndpoints
{
    private const string BaseRoute = "/api/e2e/audit-evidence";
    private const string StorageThresholdCheckRoute = "/storage-threshold/check";
    private const string IntegrityVerificationRoute = "/files/{fileId:guid}/integrity-verification";
    private const string IntegrityTamperRoute = "/files/{fileId:guid}/tamper";

    public static IEndpointRouteBuilder MapE2eAuditEvidenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints
            .MapGroup(BaseRoute)
            .RequireAuthorization(IdentityDefinitions.PlatformAdmin)
            .WithMetadata(new ModuleEndpointAntiforgeryMetadata());

        _ = group.MapPost(StorageThresholdCheckRoute, CheckStorageAboveThresholdAsync);
        _ = group.MapPost(IntegrityVerificationRoute, VerifyFileIntegrityAsync);
        _ = group.MapPost(IntegrityTamperRoute, TamperFileObjectAsync);
        return endpoints;
    }

    // Runs one real capacity check while the simulated storage size is above the threshold.
    private static async Task<IResult> CheckStorageAboveThresholdAsync(
        IServiceProvider services,
        E2eAuditCapacitySimulation simulation,
        CancellationToken cancellationToken)
    {
        AuditCapacityAlertMonitor monitor = services.GetServices<IHostedService>().OfType<AuditCapacityAlertMonitor>().Single();
        simulation.ThresholdExceeded = true;
        try
        {
            await monitor.CheckOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            simulation.ThresholdExceeded = false;
        }

        return Results.NoContent();
    }

    private static async Task<IResult> VerifyFileIntegrityAsync(
        Guid fileId,
        IFileMetadataAccess metadataAccess,
        IFileIntegrityService integrityService,
        IFileTransferAudit transferAudit,
        CancellationToken cancellationToken)
    {
        FileInformationReadDto? file = await metadataAccess.GetAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null)
            return Results.NotFound();

        FileIntegrityVerificationResult verification = await integrityService.VerifyAsync(file, cancellationToken).ConfigureAwait(false);
        await transferAudit.RecordIntegrityVerificationAsync(
            fileId,
            verification.IsVerified ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Failed,
            verification.IsVerified ? null : SecurityAuditReasonCatalog.IntegrityValidationFailed,
            cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> TamperFileObjectAsync(
        Guid fileId,
        FilesDbContext dbContext,
        IObjectStorageService storage,
        CancellationToken cancellationToken)
    {
        FileIntegrityRecord? record = await dbContext.Set<FileIntegrityRecord>()
            .SingleOrDefaultAsync(candidate => candidate.FileInformationId == fileId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
            return Results.NotFound();
        if (record.ContentLength <= 0 || record.ContentLength > int.MaxValue)
            return Results.Conflict();

        await using Stream original = await storage.DownloadAsync(record.ObjectKey, cancellationToken).ConfigureAwait(false);
        using MemoryStream buffer = new();
        await original.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        byte[] content = buffer.ToArray();
        if (content.Length == 0)
            return Results.Conflict();

        content[0] ^= 0xFF;
        await using MemoryStream tampered = new(content, writable: false);
        await storage.UploadAsync(record.ObjectKey, record.ContentType, tampered, content.Length, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
