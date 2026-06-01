using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Queries;

/// <summary>Gets sync job history for monitoring and admin dashboard.</summary>
public sealed record GetReferenceSyncJobHistoryQuery : IRequest<IReadOnlyList<ReferenceSyncJobDto>>
{
    public string? ProviderKey { get; init; }
    public string? CodeSystem { get; init; }
    public ReferenceCodeSyncStatus? Status { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}

/// <summary>Gets current sync status (in-progress or last completed).</summary>
public sealed record GetCurrentReferenceSyncStatusQuery : IRequest<CurrentSyncStatusDto?>
{
    public required string ProviderKey { get; init; }
    public required string CodeSystem { get; init; }
}

/// <summary>Gets freshness overview for all providers and code systems.</summary>
public sealed record GetReferenceFreshnessQuery : IRequest<IReadOnlyList<ReferenceFreshnessDto>>
{
    public string? ProviderKey { get; init; }
}

/// <summary>Gets change logs with breaking change detection.</summary>
public sealed record GetReferenceChangeLogsQuery : IRequest<IReadOnlyList<ReferenceChangeLogDto>>
{
    public Guid? SyncJobId { get; init; }
    public string? ProviderKey { get; init; }
    public bool? BreakingOnly { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}

// DTOs

public sealed record ReferenceSyncJobDto
{
    public Guid Id { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public string CodeSystem { get; init; } = string.Empty;
    public ReferenceCodeSyncStatus Status { get; init; }
    public string? DataVersion { get; init; }
    public int TotalItemsProcessed { get; init; }
    public int ItemsAdded { get; init; }
    public int ItemsUpdated { get; init; }
    public int ItemsDeactivated { get; init; }
    public int ErrorCount { get; init; }
    public long DurationMs { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? LastError { get; init; }
    public bool IsManualTrigger { get; init; }
    public string? TriggeredBy { get; init; }
}

public sealed record CurrentSyncStatusDto
{
    public Guid JobId { get; init; }
    public ReferenceCodeSyncStatus Status { get; init; }
    public int ProgressPercentage { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? DataVersion { get; init; }
    public int ItemsProcessed { get; init; }
}

public sealed record ReferenceFreshnessDto
{
    public string ProviderKey { get; init; } = string.Empty;
    public string CodeSystem { get; init; } = string.Empty;
    public DateTimeOffset? LastSuccessfulSync { get; init; }
    public string? LastDataVersion { get; init; }
    public int ActiveCodeCount { get; init; }
    public int TotalCodeCount { get; init; }
    public bool IsStale { get; init; }
    public ReferenceCodeSyncStatus? LastJobStatus { get; init; }
    public int RecentBreakingChanges { get; init; }
}

public sealed record ReferenceChangeLogDto
{
    public Guid Id { get; init; }
    public Guid SyncJobId { get; init; }
    public ReferenceCodeChangeType ChangeType { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public bool IsBreakingChange { get; init; }
    public int AffectedCrosswalkCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
