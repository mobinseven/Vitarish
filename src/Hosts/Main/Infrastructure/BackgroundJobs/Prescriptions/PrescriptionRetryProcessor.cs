using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Modules.Base.Platform.Retry;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Prescriptions;

/// <summary>
/// Retries SyncPending prescription submissions via the centralized outbox.
/// Picks up outbox entries due for retry and re-dispatches SubmitPrescriptionCommand.
/// </summary>
public sealed class PrescriptionRetryProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<PrescriptionRetryProcessor> logger)
{
    private const string BackgroundResource = "hangfire:prescription:retry";

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

        // Also pick up any SyncPending prescriptions that don't have an outbox entry yet
        // (e.g., from before the outbox was introduced)
        await SyncOrphanedSyncPendingAsync(outboxService, dbContext, shutdownToken);

        // Process due outbox entries
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

    private static async Task SyncOrphanedSyncPendingAsync(
        IOutboxService outboxService,
        PrescriptionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Find SyncPending prescriptions with no corresponding outbox entry
        var syncPendingStatus = PrescriptionStatus.SyncPending.ToString();
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
