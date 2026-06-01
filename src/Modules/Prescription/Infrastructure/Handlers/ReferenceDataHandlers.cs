using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Application.Queries;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Services;

namespace Vitarish.Prescription.Infrastructure.Handlers;

/// <summary>Handler for manual sync trigger command.</summary>
public sealed class SyncReferenceDataHandler(
    ReferenceDataSyncService syncService,
    IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<SyncReferenceDataCommand, SyncReferenceDataResult>
{
    public async Task<SyncReferenceDataResult> Handle(
        SyncReferenceDataCommand request,
        CancellationToken cancellationToken)
    {
        var adapter = providerRegistry.GetAdapter(request.ProviderKey);
        if (adapter is null)
            return SyncReferenceDataResult.Failure($"Provider '{request.ProviderKey}' not found");

        var capabilities = adapter.GetCapabilities();
        var supportsDrug = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceDrugSync);
        var supportsService = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceServiceSync);

        if (!supportsDrug && !supportsService)
            return SyncReferenceDataResult.Failure("Provider does not support reference data sync");

        if (request.CodeSystem.Contains("Drug", StringComparison.OrdinalIgnoreCase) && !supportsDrug)
            return SyncReferenceDataResult.Failure("Provider does not support drug code sync");

        if (request.CodeSystem.Contains("Service", StringComparison.OrdinalIgnoreCase) && !supportsService)
            return SyncReferenceDataResult.Failure("Provider does not support service code sync");

        try
        {
            var syncJob = await syncService.ExecuteSyncAsync(
                request.ProviderKey,
                request.CodeSystem,
                request.Priority,
                request.TriggeredBy,
                cancellationToken);

            return SyncReferenceDataResult.Success(
                syncJob.Id,
                syncJob.Status,
                syncJob.TotalItemsProcessed,
                (syncJob.ItemsAdded, syncJob.ItemsUpdated, syncJob.ItemsDeactivated));
        }
        catch (Exception ex)
        {
            return SyncReferenceDataResult.Failure(ex.Message);
        }
    }
}

/// <summary>Handler for retrieving sync job history.</summary>
public sealed class GetReferenceSyncJobHistoryHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReferenceSyncJobHistoryQuery, IReadOnlyList<ReferenceSyncJobDto>>
{
    public async Task<IReadOnlyList<ReferenceSyncJobDto>> Handle(
        GetReferenceSyncJobHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ReferenceCodeSyncJob>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
            query = query.Where(j => j.ProviderKey == request.ProviderKey);

        if (!string.IsNullOrEmpty(request.CodeSystem))
            query = query.Where(j => j.CodeSystem == request.CodeSystem);

        if (request.Status.HasValue)
            query = query.Where(j => j.Status == request.Status);

        return await query
            .OrderByDescending(j => j.StartedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(j => new ReferenceSyncJobDto
            {
                Id = j.Id,
                ProviderKey = j.ProviderKey,
                CodeSystem = j.CodeSystem,
                Status = j.Status,
                DataVersion = j.DataVersion,
                TotalItemsProcessed = j.TotalItemsProcessed,
                ItemsAdded = j.ItemsAdded,
                ItemsUpdated = j.ItemsUpdated,
                ItemsDeactivated = j.ItemsDeactivated,
                ErrorCount = j.ErrorCount,
                DurationMs = j.DurationMs,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                LastError = j.LastError,
                IsManualTrigger = j.IsManualTrigger,
                TriggeredBy = j.TriggeredBy
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Handler for getting current sync status.</summary>
public sealed class GetCurrentReferenceSyncStatusHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetCurrentReferenceSyncStatusQuery, CurrentSyncStatusDto?>
{
    public async Task<CurrentSyncStatusDto?> Handle(
        GetCurrentReferenceSyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.Set<ReferenceCodeSyncJob>()
            .AsNoTracking()
            .Where(j => j.ProviderKey == request.ProviderKey && j.CodeSystem == request.CodeSystem)
            .OrderByDescending(j => j.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return null;

        return new CurrentSyncStatusDto
        {
            JobId = job.Id,
            Status = job.Status,
            ProgressPercentage = job.Status == ReferenceCodeSyncStatus.InProgress ? 50 : 100,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            DataVersion = job.DataVersion,
            ItemsProcessed = job.TotalItemsProcessed
        };
    }
}

/// <summary>Handler for reference freshness overview.</summary>
public sealed class GetReferenceFreshnessHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReferenceFreshnessQuery, IReadOnlyList<ReferenceFreshnessDto>>
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromDays(2);

    public async Task<IReadOnlyList<ReferenceFreshnessDto>> Handle(
        GetReferenceFreshnessQuery request,
        CancellationToken cancellationToken)
    {
        // Get distinct provider/code system combinations from reference codes
        var combinations = await dbContext.ReferenceCodes
            .AsNoTracking()
            .Select(rc => new { rc.ProviderKey, rc.CodeSystem })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.ProviderKey))
            combinations = combinations.Where(c => c.ProviderKey == request.ProviderKey).ToList();

        var results = new List<ReferenceFreshnessDto>();
        var now = DateTimeOffset.UtcNow;

        foreach (var combo in combinations)
        {
            var lastJob = await dbContext.Set<ReferenceCodeSyncJob>()
                .AsNoTracking()
                .Where(j => j.ProviderKey == combo.ProviderKey
                    && j.CodeSystem == combo.CodeSystem
                    && (j.Status == ReferenceCodeSyncStatus.Completed || j.Status == ReferenceCodeSyncStatus.CompletedWithErrors))
                .OrderByDescending(j => j.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var activeCount = await dbContext.ReferenceCodes
                .CountAsync(rc => rc.ProviderKey == combo.ProviderKey
                    && rc.CodeSystem == combo.CodeSystem
                    && rc.IsActive, cancellationToken);

            var totalCount = await dbContext.ReferenceCodes
                .CountAsync(rc => rc.ProviderKey == combo.ProviderKey
                    && rc.CodeSystem == combo.CodeSystem, cancellationToken);

            var recentBreaking = lastJob is null ? 0 : await dbContext.Set<ReferenceCodeChangeLog>()
                .CountAsync(cl => cl.SyncJobId == lastJob.Id && cl.IsBreakingChange, cancellationToken);

            results.Add(new ReferenceFreshnessDto
            {
                ProviderKey = combo.ProviderKey,
                CodeSystem = combo.CodeSystem,
                LastSuccessfulSync = lastJob?.CompletedAt,
                LastDataVersion = lastJob?.DataVersion,
                ActiveCodeCount = activeCount,
                TotalCodeCount = totalCount,
                IsStale = lastJob is null || (now - lastJob.CompletedAt!.Value) > StalenessThreshold,
                LastJobStatus = lastJob?.Status,
                RecentBreakingChanges = recentBreaking
            });
        }

        return results;
    }
}

/// <summary>Handler for change log retrieval.</summary>
public sealed class GetReferenceChangeLogsHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReferenceChangeLogsQuery, IReadOnlyList<ReferenceChangeLogDto>>
{
    public async Task<IReadOnlyList<ReferenceChangeLogDto>> Handle(
        GetReferenceChangeLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<ReferenceCodeChangeLog>().AsNoTracking().AsQueryable();

        if (request.SyncJobId.HasValue)
            query = query.Where(cl => cl.SyncJobId == request.SyncJobId);

        if (request.BreakingOnly == true)
            query = query.Where(cl => cl.IsBreakingChange);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var jobIds = dbContext.Set<ReferenceCodeSyncJob>()
                .Where(j => j.ProviderKey == request.ProviderKey)
                .Select(j => j.Id);
            query = query.Where(cl => jobIds.Contains(cl.SyncJobId));
        }

        return await query
            .OrderByDescending(cl => cl.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(cl => new ReferenceChangeLogDto
            {
                Id = cl.Id,
                SyncJobId = cl.SyncJobId,
                ChangeType = cl.ChangeType,
                Code = cl.Code,
                OldValue = cl.OldValue,
                NewValue = cl.NewValue,
                IsBreakingChange = cl.IsBreakingChange,
                AffectedCrosswalkCount = cl.AffectedCrosswalkCount,
                CreatedAt = cl.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
