using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Application.Queries;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

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
public sealed class GetReferenceSyncJobHistoryHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<GetReferenceSyncJobHistoryQuery, IReadOnlyList<ReferenceSyncJobDto>>
{
    public async Task<IReadOnlyList<ReferenceSyncJobDto>> Handle(
        GetReferenceSyncJobHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = requestContext.TenantId;
        var query = dbContext.Set<ReferenceCodeSyncJob>().AsNoTracking()
            .Where(j => j.TenantId == tenantId)
            .AsQueryable();

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
public sealed class GetCurrentReferenceSyncStatusHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<GetCurrentReferenceSyncStatusQuery, CurrentSyncStatusDto?>
{
    public async Task<CurrentSyncStatusDto?> Handle(
        GetCurrentReferenceSyncStatusQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = requestContext.TenantId;
        var job = await dbContext.Set<ReferenceCodeSyncJob>()
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.ProviderKey == request.ProviderKey && j.CodeSystem == request.CodeSystem)
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
public sealed class GetReferenceFreshnessHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IRequestContext requestContext)
    : IRequestHandler<GetReferenceFreshnessQuery, IReadOnlyList<ReferenceFreshnessDto>>
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromDays(2);

    public async Task<IReadOnlyList<ReferenceFreshnessDto>> Handle(
        GetReferenceFreshnessQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = requestContext.TenantId;

        // Derive provider/code-system combinations from registered providers so the dashboard
        // works even before the first sync (empty ReferenceCodes table).
        var registeredCombinations = providerRegistry.GetRegisteredProviderKeys()
            .SelectMany(key =>
            {
                var adapter = providerRegistry.GetAdapter(key);
                if (adapter is null) return Enumerable.Empty<(string, string)>();
                var caps = adapter.GetCapabilities();
                var combos = new List<(string, string)>();
                if (caps.Contains(InsuranceProviderCapabilityType.SupportsReferenceDrugSync))
                    combos.Add((key, "Drug"));
                if (caps.Contains(InsuranceProviderCapabilityType.SupportsReferenceServiceSync))
                    combos.Add((key, "Service"));
                return combos;
            })
            .ToList();

        // Also include any combinations seen in sync job history that are not in the registered list
        var historyCombinations = await dbContext.Set<ReferenceCodeSyncJob>()
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId)
            .Select(j => new { j.ProviderKey, j.CodeSystem })
            .Distinct()
            .ToListAsync(cancellationToken);

        var combinations = registeredCombinations
            .Union(historyCombinations.Select(c => (c.ProviderKey, c.CodeSystem)))
            .ToList();

        if (!string.IsNullOrEmpty(request.ProviderKey))
            combinations = combinations.Where(c => c.Item1 == request.ProviderKey).ToList();

        if (combinations.Count == 0)
            return [];

        // Precompute last completed job per (ProviderKey, CodeSystem) in one query
        var providerKeys = combinations.Select(c => c.Item1).ToHashSet();
        var codeSystems = combinations.Select(c => c.Item2).ToHashSet();

        var allLastJobs = await dbContext.Set<ReferenceCodeSyncJob>()
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId
                && providerKeys.Contains(j.ProviderKey)
                && codeSystems.Contains(j.CodeSystem)
                && (j.Status == ReferenceCodeSyncStatus.Completed || j.Status == ReferenceCodeSyncStatus.CompletedWithErrors))
            .GroupBy(j => new { j.ProviderKey, j.CodeSystem })
            .Select(g => g.OrderByDescending(j => j.CompletedAt).First())
            .ToListAsync(cancellationToken);

        var lastJobLookup = allLastJobs.ToDictionary(j => (j.ProviderKey, j.CodeSystem));

        // Precompute active and total code counts per (ProviderKey, CodeSystem)
        var activeCounts = await dbContext.ReferenceCodes
            .AsNoTracking()
            .Where(rc => providerKeys.Contains(rc.ProviderKey) && codeSystems.Contains(rc.CodeSystem) && rc.IsActive)
            .GroupBy(rc => new { rc.ProviderKey, rc.CodeSystem })
            .Select(g => new { g.Key.ProviderKey, g.Key.CodeSystem, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalCounts = await dbContext.ReferenceCodes
            .AsNoTracking()
            .Where(rc => providerKeys.Contains(rc.ProviderKey) && codeSystems.Contains(rc.CodeSystem))
            .GroupBy(rc => new { rc.ProviderKey, rc.CodeSystem })
            .Select(g => new { g.Key.ProviderKey, g.Key.CodeSystem, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var activeCountLookup = activeCounts.ToDictionary(x => (x.ProviderKey, x.CodeSystem), x => x.Count);
        var totalCountLookup = totalCounts.ToDictionary(x => (x.ProviderKey, x.CodeSystem), x => x.Count);

        // Precompute recent breaking change counts per last job id
        var lastJobIds = lastJobLookup.Values.Select(j => j.Id).ToHashSet();
        var breakingCounts = lastJobIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.Set<ReferenceCodeChangeLog>()
                .AsNoTracking()
                .Where(cl => lastJobIds.Contains(cl.SyncJobId) && cl.IsBreakingChange)
                .GroupBy(cl => cl.SyncJobId)
                .Select(g => new { SyncJobId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SyncJobId, x => x.Count, cancellationToken);

        var results = new List<ReferenceFreshnessDto>();
        var now = DateTimeOffset.UtcNow;

        foreach (var (providerKey, codeSystem) in combinations)
        {
            lastJobLookup.TryGetValue((providerKey, codeSystem), out var lastJob);
            activeCountLookup.TryGetValue((providerKey, codeSystem), out var activeCount);
            totalCountLookup.TryGetValue((providerKey, codeSystem), out var totalCount);
            var recentBreaking = lastJob is null ? 0 : breakingCounts.GetValueOrDefault(lastJob.Id, 0);

            results.Add(new ReferenceFreshnessDto
            {
                ProviderKey = providerKey,
                CodeSystem = codeSystem,
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
public sealed class GetReferenceChangeLogsHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<GetReferenceChangeLogsQuery, IReadOnlyList<ReferenceChangeLogDto>>
{
    public async Task<IReadOnlyList<ReferenceChangeLogDto>> Handle(
        GetReferenceChangeLogsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = requestContext.TenantId;

        // Scope change log access to tenant-owned sync jobs
        var tenantJobIds = dbContext.Set<ReferenceCodeSyncJob>()
            .Where(j => j.TenantId == tenantId)
            .Select(j => j.Id);

        var query = dbContext.Set<ReferenceCodeChangeLog>().AsNoTracking()
            .Where(cl => tenantJobIds.Contains(cl.SyncJobId))
            .AsQueryable();

        if (request.SyncJobId.HasValue)
            query = query.Where(cl => cl.SyncJobId == request.SyncJobId);

        if (request.BreakingOnly == true)
            query = query.Where(cl => cl.IsBreakingChange);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var jobIds = dbContext.Set<ReferenceCodeSyncJob>()
                .Where(j => j.TenantId == tenantId && j.ProviderKey == request.ProviderKey)
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
