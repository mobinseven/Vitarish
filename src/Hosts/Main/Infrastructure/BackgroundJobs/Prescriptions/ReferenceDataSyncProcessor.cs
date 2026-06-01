using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Services;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Prescriptions;

/// <summary>
/// Processes scheduled reference data sync jobs.
/// Runs per-tenant, picks up pending sync jobs and executes them.
/// Sync failure for one provider doesn't affect others.
/// </summary>
public sealed class ReferenceDataSyncProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<ReferenceDataSyncProcessor> logger)
{
    private const string BackgroundResource = "hangfire:prescription:reference-sync";

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        var syncService = provider.GetRequiredService<ReferenceDataSyncService>();
        var dbContext = provider.GetRequiredService<PrescriptionDbContext>();
        var providerRegistry = provider.GetRequiredService<IInsuranceProviderRegistry>();

        // Get all registered providers and check which ones support reference sync
        var providerKeys = providerRegistry.GetRegisteredProviderKeys();

        foreach (var providerKey in providerKeys)
        {
            shutdownToken.ThrowIfCancellationRequested();

            var adapter = providerRegistry.GetAdapter(providerKey);
            if (adapter is null) continue;

            var capabilities = adapter.GetCapabilities();

            // Sync drug codes if supported
            if (capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceDrugSync))
            {
                await TrySyncAsync(syncService, providerKey, "Drug", shutdownToken);
            }

            // Sync service codes if supported
            if (capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceServiceSync))
            {
                await TrySyncAsync(syncService, providerKey, "Service", shutdownToken);
            }
        }
    }

    private async Task TrySyncAsync(
        ReferenceDataSyncService syncService,
        string providerKey,
        string codeSystem,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Starting reference sync for {Provider}/{CodeSystem}",
                providerKey, codeSystem);

            var result = await syncService.ExecuteSyncAsync(
                providerKey,
                codeSystem,
                SyncPriority.Normal,
                "hangfire:schedule",
                cancellationToken);

            logger.LogInformation(
                "Reference sync for {Provider}/{CodeSystem} completed with status {Status}: {Added} added, {Updated} updated, {Deactivated} deactivated",
                providerKey, codeSystem, result.Status, result.ItemsAdded, result.ItemsUpdated, result.ItemsDeactivated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Reference sync for {Provider}/{CodeSystem} threw exception",
                providerKey, codeSystem);
        }
    }
}
