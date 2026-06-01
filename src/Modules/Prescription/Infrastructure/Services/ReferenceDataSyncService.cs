using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Services;

/// <summary>
/// Executes reference data sync operations: fetches data from provider adapters,
/// performs versioned import (old codes deactivated, not deleted), and tracks change logs.
/// Sync failure for one provider doesn't affect others.
/// </summary>
public sealed class ReferenceDataSyncService(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IRequestContext requestContext,
    ILogger<ReferenceDataSyncService> logger)
{
    /// <summary>
    /// Executes a full sync for a provider + code system.
    /// Returns the completed sync job record.
    /// </summary>
    public async Task<ReferenceCodeSyncJob> ExecuteSyncAsync(
        string providerKey,
        string codeSystem,
        SyncPriority priority,
        string? triggeredBy,
        CancellationToken cancellationToken)
    {
        var syncJob = new ReferenceCodeSyncJob
        {
            Id = Guid.NewGuid(),
            ProviderKey = providerKey,
            CodeSystem = codeSystem,
            Status = ReferenceCodeSyncStatus.InProgress,
            Priority = priority,
            TriggeredBy = triggeredBy,
            IsManualTrigger = triggeredBy?.StartsWith("admin:") == true,
            TenantId = requestContext.TenantId,
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Add(syncJob);
        await dbContext.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var adapter = providerRegistry.GetAdapter(providerKey);
            if (adapter is null)
            {
                return await FailSyncJob(syncJob, "Provider adapter not found", stopwatch, cancellationToken);
            }

            // Get current version for delta request
            var lastSuccessfulJob = await dbContext.Set<ReferenceCodeSyncJob>()
                .Where(j => j.ProviderKey == providerKey
                    && j.CodeSystem == codeSystem
                    && (j.Status == ReferenceCodeSyncStatus.Completed || j.Status == ReferenceCodeSyncStatus.CompletedWithErrors))
                .OrderByDescending(j => j.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);

            syncJob.PreviousDataVersion = lastSuccessfulJob?.DataVersion;

            // Fetch reference data from provider
            var fetchResult = await adapter.FetchReferenceDataAsync(new ReferenceDataRequest
            {
                CodeSystem = codeSystem,
                SinceVersion = syncJob.PreviousDataVersion
            }, cancellationToken);

            if (!fetchResult.IsSuccess || fetchResult.Data is null)
            {
                var errorMsg = fetchResult.IsNotSupported
                    ? $"Provider '{providerKey}' does not support reference data fetch for '{codeSystem}'"
                    : fetchResult.Error?.UserMessage ?? "Unknown fetch error";
                return await FailSyncJob(syncJob, errorMsg, stopwatch, cancellationToken);
            }

            // Process the fetched items
            var (added, updated, deactivated, errors) = await ProcessSyncItemsAsync(
                syncJob.Id, providerKey, codeSystem, fetchResult.Data.Items, cancellationToken);

            syncJob.DataVersion = fetchResult.Data.Version;
            syncJob.TotalItemsProcessed = fetchResult.Data.Items.Count;
            syncJob.ItemsAdded = added;
            syncJob.ItemsUpdated = updated;
            syncJob.ItemsDeactivated = deactivated;
            syncJob.ErrorCount = errors;
            syncJob.DurationMs = stopwatch.ElapsedMilliseconds;
            syncJob.CompletedAt = DateTimeOffset.UtcNow;
            syncJob.Status = errors > 0
                ? ReferenceCodeSyncStatus.CompletedWithErrors
                : ReferenceCodeSyncStatus.Completed;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Reference sync completed for {Provider}/{CodeSystem}: {Added} added, {Updated} updated, {Deactivated} deactivated, {Errors} errors",
                providerKey, codeSystem, added, updated, deactivated, errors);

            return syncJob;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reference sync failed for {Provider}/{CodeSystem}", providerKey, codeSystem);
            return await FailSyncJob(syncJob, ex.Message, stopwatch, cancellationToken);
        }
    }

    private async Task<(int added, int updated, int deactivated, int errors)> ProcessSyncItemsAsync(
        Guid syncJobId,
        string providerKey,
        string codeSystem,
        IReadOnlyList<ReferenceDataItem> items,
        CancellationToken cancellationToken)
    {
        int added = 0, updated = 0, deactivated = 0, errors = 0;
        var changeLogs = new List<ReferenceCodeChangeLog>();
        var syncErrors = new List<ReferenceCodeSyncError>();
        var now = DateTimeOffset.UtcNow;

        // Get existing sync-owned codes for this provider + code system
        // Filtering to RecordOrigin == "sync" ensures a deterministic single row per Code
        // and avoids duplicate key errors when manual/import records share the same Code.
        var existingCodes = await dbContext.ReferenceCodes
            .Where(rc => rc.ProviderKey == providerKey && rc.CodeSystem == codeSystem && rc.RecordOrigin == "sync")
            .ToListAsync(cancellationToken);

        var existingLookup = existingCodes.ToDictionary(c => c.Code);
        var processedCodes = new HashSet<string>();

        foreach (var item in items)
        {
            try
            {
                processedCodes.Add(item.Code);
                var checksum = ComputeChecksum($"{item.Code}|{item.DisplayName}|{item.Category}|{item.IsActive}");

                if (existingLookup.TryGetValue(item.Code, out var existing))
                {
                    // Code already exists — check for changes
                    if (existing.Checksum != checksum)
                    {
                        var oldValue = $"{existing.DisplayName}|{existing.Category}|{existing.IsActive}";
                        var newValue = $"{item.DisplayName}|{item.Category}|{item.IsActive}";

                        var wasActive = existing.IsActive;

                        existing.DisplayName = item.DisplayName;
                        existing.Category = item.Category;
                        existing.IsActive = item.IsActive;
                        existing.Checksum = checksum;
                        existing.UpdatedAt = now;
                        existing.RecordOrigin = "sync";

                        var changeType = !wasActive && item.IsActive
                            ? ReferenceCodeChangeType.Reactivated
                            : ReferenceCodeChangeType.Updated;

                        changeLogs.Add(new ReferenceCodeChangeLog
                        {
                            Id = Guid.NewGuid(),
                            SyncJobId = syncJobId,
                            ReferenceCodeId = existing.Id,
                            ChangeType = changeType,
                            Code = item.Code,
                            OldValue = oldValue,
                            NewValue = newValue,
                            CreatedAt = now
                        });

                        updated++;
                    }
                }
                else
                {
                    // New code — add it
                    var newCode = new ReferenceCode
                    {
                        Id = Guid.NewGuid(),
                        ProviderKey = providerKey,
                        CodeSystem = codeSystem,
                        Code = item.Code,
                        DisplayName = item.DisplayName,
                        Category = item.Category,
                        IsActive = item.IsActive,
                        Checksum = checksum,
                        RecordOrigin = "sync",
                        CreatedAt = now
                    };

                    dbContext.Add(newCode);

                    changeLogs.Add(new ReferenceCodeChangeLog
                    {
                        Id = Guid.NewGuid(),
                        SyncJobId = syncJobId,
                        ReferenceCodeId = newCode.Id,
                        ChangeType = ReferenceCodeChangeType.Added,
                        Code = item.Code,
                        NewValue = $"{item.DisplayName}|{item.Category}",
                        CreatedAt = now
                    });

                    added++;
                }
            }
            catch (Exception ex)
            {
                syncErrors.Add(new ReferenceCodeSyncError
                {
                    Id = Guid.NewGuid(),
                    SyncJobId = syncJobId,
                    Code = item.Code,
                    ErrorMessage = ex.Message,
                    IsRecoverable = true,
                    CreatedAt = now
                });
                errors++;
            }
        }

        // Deactivate codes not present in the sync response (versioned deactivation, not deletion)
        var codesToDeactivate = existingCodes.Where(c => c.IsActive && !processedCodes.Contains(c.Code)).ToList();

        // Precompute affected crosswalk counts in a single query to avoid N+1
        var deactivatingCodes = codesToDeactivate.Select(c => c.Code).ToHashSet();
        var crosswalkCountsByCode = await dbContext.ReferenceCodeCrosswalks
            .Where(cw => cw.ProviderKey == providerKey
                && cw.ProviderCodeSystem == codeSystem
                && deactivatingCodes.Contains(cw.ProviderCode)
                && cw.IsActive)
            .GroupBy(cw => cw.ProviderCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count, cancellationToken);

        foreach (var code in codesToDeactivate)
        {
            code.IsActive = false;
            code.UpdatedAt = now;

            crosswalkCountsByCode.TryGetValue(code.Code, out var affectedCount);

            changeLogs.Add(new ReferenceCodeChangeLog
            {
                Id = Guid.NewGuid(),
                SyncJobId = syncJobId,
                ReferenceCodeId = code.Id,
                ChangeType = ReferenceCodeChangeType.Deactivated,
                Code = code.Code,
                OldValue = "Active",
                NewValue = "Inactive",
                IsBreakingChange = affectedCount > 0,
                AffectedCrosswalkCount = affectedCount,
                CreatedAt = now
            });

            deactivated++;
        }

        // Bulk insert change logs and errors
        dbContext.AddRange(changeLogs);
        dbContext.AddRange(syncErrors);

        await dbContext.SaveChangesAsync(cancellationToken);

        return (added, updated, deactivated, errors);
    }

    private async Task<ReferenceCodeSyncJob> FailSyncJob(
        ReferenceCodeSyncJob syncJob, string error, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        syncJob.Status = ReferenceCodeSyncStatus.Failed;
        syncJob.LastError = error;
        syncJob.DurationMs = stopwatch.ElapsedMilliseconds;
        syncJob.CompletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return syncJob;
    }

    /// <summary>Computes SHA256 checksum for change detection.</summary>
    private static string ComputeChecksum(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
