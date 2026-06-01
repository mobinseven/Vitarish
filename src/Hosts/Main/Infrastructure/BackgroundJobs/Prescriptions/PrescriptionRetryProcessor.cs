using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Modules.Base.Platform.Retry;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Prescriptions;

/// <summary>
/// Retries SyncPending prescription submissions via the centralized outbox.
/// Implements fetch-before-retry: checks provider status before re-submitting to avoid duplicates.
/// Escalates stale SyncPending records to DuplicateRisk or SyncFailed.
/// </summary>
public sealed class PrescriptionRetryProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<PrescriptionRetryProcessor> logger)
{
    private const string BackgroundResource = "hangfire:prescription:retry";

    /// <summary>
    /// Maximum age for a SyncPending prescription before it is escalated.
    /// </summary>
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(2);

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        var outboxService = provider.GetRequiredService<IOutboxService>();
        var mediator = provider.GetRequiredService<IMediator>();
        var dbContext = provider.GetRequiredService<PrescriptionDbContext>();
        var providerRegistry = provider.GetRequiredService<IInsuranceProviderRegistry>();

        // Phase 1: Escalate stale SyncPending prescriptions
        await EscalateStaleSyncPendingAsync(dbContext, outboxService, shutdownToken);

        // Phase 2: Pick up any SyncPending prescriptions without outbox entries
        await SyncOrphanedSyncPendingAsync(outboxService, dbContext, shutdownToken);

        // Phase 3: Process due outbox entries with fetch-before-retry
        var entries = await outboxService.GetPendingAsync(maxItems: 20, cancellationToken: shutdownToken);

        foreach (var entry in entries)
        {
            shutdownToken.ThrowIfCancellationRequested();

            if (entry.OperationName != "SubmitPrescription")
                continue;

            try
            {
                var payload = JsonSerializer.Deserialize<SubmitPayload>(entry.PayloadJson);
                if (payload is null)
                {
                    await outboxService.FailAsync(entry.Id, "Invalid payload JSON", shutdownToken);
                    continue;
                }

                // Mark processing
                await dbContext.OutboxRecords
                    .Where(r => r.Id == entry.Id)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(r => r.Status, OutboxEntryStatus.Processing.ToString())
                              .SetProperty(r => r.LastAttemptAt, DateTimeOffset.UtcNow),
                        shutdownToken);

                // Fetch-before-retry: check if provider already has this prescription
                var fetchResolved = await TryFetchBeforeRetryAsync(
                    payload.PrescriptionId, dbContext, providerRegistry, entry, outboxService, shutdownToken);

                if (fetchResolved)
                    continue;

                // Provider doesn't have it or fetch failed — safe to retry submission
                var result = await mediator.Send(new SubmitPrescriptionCommand
                {
                    PrescriptionId = payload.PrescriptionId,
                    CorrelationId = payload.CorrelationId ?? Guid.NewGuid().ToString()
                }, shutdownToken);

                if (result.IsSuccess)
                {
                    await outboxService.CompleteAsync(entry.Id, shutdownToken);
                    logger.LogInformation(
                        "Prescription {PrescriptionId} retry succeeded via outbox entry {EntryId}",
                        payload.PrescriptionId, entry.Id);
                }
                else
                {
                    await outboxService.FailAsync(entry.Id, result.ErrorMessage ?? "Retry failed", shutdownToken);
                    logger.LogWarning(
                        "Prescription {PrescriptionId} retry failed (attempt {Attempt}/{Max}): {Error}",
                        payload.PrescriptionId, entry.RetryCount + 1, entry.MaxRetries, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                await outboxService.FailAsync(entry.Id, ex.Message, shutdownToken);
                logger.LogError(ex,
                    "Prescription retry processor threw while handling outbox entry {EntryId}",
                    entry.Id);
            }
        }
    }

    /// <summary>
    /// Fetch-before-retry: checks with the provider if the prescription was already accepted.
    /// Returns true if the fetch resolved the prescription status (no retry needed).
    /// </summary>
    private async Task<bool> TryFetchBeforeRetryAsync(
        Guid prescriptionId,
        PrescriptionDbContext dbContext,
        IInsuranceProviderRegistry providerRegistry,
        OutboxEntry entry,
        IOutboxService outboxService,
        CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

        if (prescription is null || string.IsNullOrEmpty(prescription.SelectedProviderKey))
            return false;

        var adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
            return false;

        // Check if adapter supports fetch
        if (!adapter.GetCapabilities().Contains(InsuranceProviderCapabilityType.SupportsFetchPrescription))
            return false;

        // Get the last known remote ID
        var submission = await dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescriptionId)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Try to fetch from provider
        var fetchResult = await adapter.FetchPrescriptionAsync(new FetchRequest
        {
            RemotePrescriptionId = submission?.RemotePrescriptionId,
            CorrelationId = entry.CorrelationId
        }, cancellationToken);

        if (!fetchResult.IsSuccess || fetchResult.Data is null)
            return false; // Fetch failed or no data — proceed with retry

        var remoteStatus = fetchResult.Data.CanonicalStatus;

        // If provider says it's accepted, we're done
        if (remoteStatus == PrescriptionStatus.AcceptedByProvider ||
            remoteStatus == PrescriptionStatus.Submitted)
        {
            if (PrescriptionRecord.IsValidTransition(prescription.Status, remoteStatus))
            {
                prescription.TransitionTo(remoteStatus);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await outboxService.CompleteAsync(entry.Id, cancellationToken);
            logger.LogInformation(
                "Prescription {PrescriptionId} resolved via fetch-before-retry: provider status {Status}",
                prescriptionId, remoteStatus);
            return true;
        }

        // If provider says it's rejected, mark it
        if (remoteStatus == PrescriptionStatus.RejectedByProvider)
        {
            if (PrescriptionRecord.IsValidTransition(prescription.Status, PrescriptionStatus.RejectedByProvider))
            {
                prescription.TransitionTo(PrescriptionStatus.RejectedByProvider);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await outboxService.CompleteAsync(entry.Id, cancellationToken);
            logger.LogInformation(
                "Prescription {PrescriptionId} rejected by provider (fetch-before-retry)",
                prescriptionId);
            return true;
        }

        // Provider returned an inconclusive status — check for duplicate risk
        if (entry.RetryCount >= entry.MaxRetries - 1)
        {
            // Last retry and provider has some record — mark as DuplicateRisk
            await outboxService.MarkDuplicateRiskAsync(entry.Id,
                $"Provider returned status '{fetchResult.Data.RemoteStatusCode}' after max retries", cancellationToken);

            if (PrescriptionRecord.IsValidTransition(prescription.Status, PrescriptionStatus.DuplicateRisk))
            {
                prescription.TransitionTo(PrescriptionStatus.DuplicateRisk);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogWarning(
                "Prescription {PrescriptionId} marked as DuplicateRisk: provider has record with status '{Status}'",
                prescriptionId, fetchResult.Data.RemoteStatusCode);
            return true;
        }

        return false; // Status inconclusive and retries remain — proceed with retry
    }

    /// <summary>
    /// Escalates SyncPending prescriptions that have been stuck beyond the stale threshold.
    /// </summary>
    private async Task EscalateStaleSyncPendingAsync(
        PrescriptionDbContext dbContext,
        IOutboxService outboxService,
        CancellationToken cancellationToken)
    {
        var staleThreshold = DateTimeOffset.UtcNow - StaleThreshold;

        // Find stale SyncPending prescriptions
        var stalePrescriptions = await dbContext.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.SyncPending)
            .Where(p => (p.UpdatedAt ?? p.SubmittedAt ?? p.CreatedAt) < staleThreshold)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var prescription in stalePrescriptions)
        {
            // Check if there's a failed outbox entry (all retries exhausted)
            var hasExhaustedEntry = await dbContext.OutboxRecords
                .AnyAsync(o =>
                    o.PrescriptionId == prescription.Id &&
                    o.Status == OutboxEntryStatus.Failed.ToString(),
                    cancellationToken);

            var targetStatus = hasExhaustedEntry
                ? PrescriptionStatus.SyncFailed
                : PrescriptionStatus.DuplicateRisk;

            if (PrescriptionRecord.IsValidTransition(prescription.Status, targetStatus))
            {
                prescription.TransitionTo(targetStatus);

                logger.LogWarning(
                    "Stale SyncPending prescription {PrescriptionId} escalated to {Status} (stuck since {UpdatedAt})",
                    prescription.Id, targetStatus, prescription.UpdatedAt ?? prescription.SubmittedAt);
            }
        }

        if (stalePrescriptions.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SyncOrphanedSyncPendingAsync(
        IOutboxService outboxService,
        PrescriptionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Find SyncPending prescriptions with no corresponding outbox entry
        var pendingOutboxStatus = OutboxEntryStatus.Pending.ToString();
        var processingStatus = OutboxEntryStatus.Processing.ToString();

        var orphaned = await dbContext.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.SyncPending)
            .Where(p => !dbContext.OutboxRecords.Any(o =>
                o.PrescriptionId == p.Id &&
                (o.Status == pendingOutboxStatus || o.Status == processingStatus)))
            .Take(20)
            .Select(p => new { p.Id, p.SelectedProviderKey, p.VersionNumber, p.TenantId })
            .ToListAsync(cancellationToken);

        foreach (var p in orphaned)
        {
            if (string.IsNullOrEmpty(p.SelectedProviderKey)) continue;

            _ = await outboxService.EnqueueAsync(new OutboxEntry
            {
                ProviderKey = p.SelectedProviderKey,
                OperationName = "SubmitPrescription",
                PayloadJson = JsonSerializer.Serialize(new { PrescriptionId = p.Id, CorrelationId = Guid.NewGuid().ToString() }),
                IdempotencyKey = $"{p.Id}:v{p.VersionNumber}",
                CorrelationId = Guid.NewGuid().ToString(),
                NextRetryAt = DateTimeOffset.UtcNow,
                TenantId = p.TenantId
            }, cancellationToken);
        }
    }

    private sealed record SubmitPayload
    {
        public Guid PrescriptionId { get; init; }
        public string? CorrelationId { get; init; }
    }
}
