using MediatR;
using Vitarish.Medical.Domain.Enums;

namespace Vitarish.Medical.Application.Queries.Prescriptions;

/// <summary>
/// Gets provider-aware API transaction metrics for the operational dashboard.
/// Includes success/failure rates, latency percentiles, and error category distribution.
/// </summary>
public sealed record GetProviderTransactionMetricsQuery : IRequest<IReadOnlyList<ProviderTransactionMetricsDto>>
{
    public string? ProviderKey { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

/// <summary>
/// Gets rejection distribution grouped by provider and error category.
/// </summary>
public sealed record GetRejectionDistributionQuery : IRequest<IReadOnlyList<RejectionDistributionDto>>
{
    public string? ProviderKey { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

/// <summary>
/// Gets credential health status for all doctor-provider credentials.
/// Surfaces expiration warnings and authentication failures.
/// </summary>
public sealed record GetCredentialHealthQuery : IRequest<CredentialHealthResult>
{
    public string? ProviderKey { get; init; }
    public Guid? ClinicId { get; init; }
}

/// <summary>
/// Gets operational alerts for the prescription module.
/// Combines credential, staleness, outage, and queue depth alerts.
/// </summary>
public sealed record GetOperationalAlertsQuery : IRequest<IReadOnlyList<OperationalAlertDto>>
{
    public string? ProviderKey { get; init; }
}

/// <summary>
/// Gets prescription status distribution for monitoring.
/// </summary>
public sealed record GetPrescriptionStatusDistributionQuery : IRequest<IReadOnlyList<StatusDistributionDto>>
{
    public string? ProviderKey { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

/// <summary>
/// Gets duplicate-risk incident history for monitoring.
/// </summary>
public sealed record GetDuplicateRiskIncidentsQuery : IRequest<DuplicateRiskIncidentsResult>
{
    public string? ProviderKey { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Take { get; init; } = 20;
}

// DTOs

public sealed record ProviderTransactionMetricsDto
{
    public string ProviderKey { get; init; } = string.Empty;
    public string OperationName { get; init; } = string.Empty;
    public int TotalCalls { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double SuccessRate { get; init; }
    public long AvgLatencyMs { get; init; }
    public long P95LatencyMs { get; init; }
    public long MaxLatencyMs { get; init; }
    public int TimeoutCount { get; init; }
    public DateTime? LastCallAt { get; init; }
}

public sealed record RejectionDistributionDto
{
    public string ProviderKey { get; init; } = string.Empty;
    public string ErrorCategory { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
    public DateTime? LastOccurrence { get; init; }
}

public sealed record CredentialHealthResult
{
    public IReadOnlyList<CredentialHealthDto> Credentials { get; init; } = [];
    public int TotalActive { get; init; }
    public int ExpiringSoon { get; init; }
    public int Expired { get; init; }
    public int Revoked { get; init; }
}

public sealed record CredentialHealthDto
{
    public Guid CredentialId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid ClinicId { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public CredentialStatus Status { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? TokenExpiresAt { get; init; }
    public int DaysUntilExpiry { get; init; }
    public bool IsExpiringSoon { get; init; }
}

public sealed record OperationalAlertDto
{
    public required string AlertType { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? ProviderKey { get; init; }
    public DateTime DetectedAt { get; init; }
    public string? ActionUrl { get; init; }
}

public sealed record StatusDistributionDto
{
    public PrescriptionStatus Status { get; init; }
    public string? ProviderKey { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
}

public sealed record DuplicateRiskIncidentsResult
{
    public int TotalIncidents { get; init; }
    public int ResolvedCount { get; init; }
    public int PendingCount { get; init; }
    public IReadOnlyList<DuplicateRiskIncidentDto> RecentIncidents { get; init; } = [];
}

public sealed record DuplicateRiskIncidentDto
{
    public Guid PrescriptionId { get; init; }
    public string? ProviderKey { get; init; }
    public PrescriptionStatus CurrentStatus { get; init; }
    public DateTime DetectedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}
