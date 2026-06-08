using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Infrastructure.Data;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

internal sealed class ObservabilityQueryHandlers(MedicalDbContext dbContext) :
    IRequestHandler<GetProviderTransactionMetricsQuery, IReadOnlyList<ProviderTransactionMetricsDto>>,
    IRequestHandler<GetRejectionDistributionQuery, IReadOnlyList<RejectionDistributionDto>>,
    IRequestHandler<GetCredentialHealthQuery, CredentialHealthResult>,
    IRequestHandler<GetOperationalAlertsQuery, IReadOnlyList<OperationalAlertDto>>,
    IRequestHandler<GetPrescriptionStatusDistributionQuery, IReadOnlyList<StatusDistributionDto>>,
    IRequestHandler<GetDuplicateRiskIncidentsQuery, DuplicateRiskIncidentsResult>
{
    private static readonly TimeSpan CredentialExpirySoonThreshold = TimeSpan.FromDays(7);
    private static readonly int RetryQueueDepthWarning = 10;
    private static readonly int RetryQueueDepthCritical = 50;

    public async Task<IReadOnlyList<ProviderTransactionMetricsDto>> Handle(
        GetProviderTransactionMetricsQuery request, CancellationToken cancellationToken)
    {
        // Derive transaction metrics from PrescriptionProviderSubmission records
        IQueryable<PrescriptionProviderSubmission> query = dbContext.PrescriptionSubmissions.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(s => s.ProviderKey == request.ProviderKey);
        }

        if (request.From.HasValue)
        {
            query = query.Where(s => s.SubmittedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(s => s.SubmittedAt <= request.To.Value);
        }

        List<PrescriptionProviderSubmission> submissions = await query.ToListAsync(cancellationToken);

        List<ProviderTransactionMetricsDto> metrics = submissions
            .GroupBy(s => new { s.ProviderKey, s.OperationName })
            .Select(g =>
            {
                int successCount = g.Count(s => s.CanonicalStatus is PrescriptionStatus.AcceptedByProvider or
                                               PrescriptionStatus.Submitted);
                int failureCount = g.Count(s => s.CanonicalStatus is PrescriptionStatus.RejectedByProvider or
                                               PrescriptionStatus.SyncFailed);
                int total = g.Count();

                return new ProviderTransactionMetricsDto
                {
                    ProviderKey = g.Key.ProviderKey ?? "Unknown",
                    OperationName = g.Key.OperationName ?? "Submit",
                    TotalCalls = total,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    SuccessRate = total > 0 ? (double)successCount / total * 100 : 0,
                    AvgLatencyMs = 0,
                    P95LatencyMs = 0,
                    MaxLatencyMs = 0,
                    TimeoutCount = g.Count(s => s.RemoteStatusCode == "TIMEOUT"),
                    LastCallAt = g.Max(s => s.SubmittedAt)
                };
            })
            .OrderBy(m => m.ProviderKey)
            .ThenBy(m => m.OperationName)
            .ToList();

        return metrics;
    }

    public async Task<IReadOnlyList<RejectionDistributionDto>> Handle(
        GetRejectionDistributionQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionProviderSubmission> query = dbContext.PrescriptionSubmissions
            .Where(s => s.CanonicalStatus == PrescriptionStatus.RejectedByProvider);

        if (request.From.HasValue)
        {
            query = query.Where(s => s.SubmittedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(s => s.SubmittedAt <= request.To.Value);
        }

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(r => r.ProviderKey == request.ProviderKey);
        }

        List<PrescriptionProviderSubmission> rejections = await query.ToListAsync(cancellationToken);
        int totalRejections = rejections.Count;

        List<RejectionDistributionDto> distribution = rejections
            .GroupBy(r => new { r.ProviderKey, ErrorCategory = r.RemoteStatusCode ?? "Unknown" })
            .Select(g => new RejectionDistributionDto
            {
                ProviderKey = g.Key.ProviderKey,
                ErrorCategory = g.Key.ErrorCategory,
                Count = g.Count(),
                Percentage = totalRejections > 0 ? (double)g.Count() / totalRejections * 100 : 0,
                LastOccurrence = g.Max(r => r.SubmittedAt)
            })
            .OrderByDescending(d => d.Count)
            .ToList();

        return distribution;
    }

    public async Task<CredentialHealthResult> Handle(
        GetCredentialHealthQuery request, CancellationToken cancellationToken)
    {
        IQueryable<DoctorProviderCredential> query = dbContext.DoctorProviderCredentials.AsQueryable();

        if (request.ClinicId.HasValue)
        {
            query = query.Where(c => c.ClinicId == request.ClinicId.Value);
        }

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(c => c.ProviderKey == request.ProviderKey);
        }

        List<DoctorProviderCredential> credentials = await query.ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        DateTime soonThreshold = now.Add(CredentialExpirySoonThreshold);

        List<CredentialHealthDto> healthDtos = credentials.Select(c =>
        {
            int daysUntilExpiry = c.ExpiresAt.HasValue
                ? (int)(c.ExpiresAt.Value - now).TotalDays
                : int.MaxValue;

            return new CredentialHealthDto
            {
                CredentialId = c.Id,
                DoctorId = c.DoctorId,
                ClinicId = c.ClinicId,
                ProviderKey = c.ProviderKey,
                Status = c.Status,
                ExpiresAt = c.ExpiresAt,
                TokenExpiresAt = c.TokenExpiresAt,
                DaysUntilExpiry = daysUntilExpiry,
                IsExpiringSoon = c.ExpiresAt.HasValue && c.ExpiresAt.Value <= soonThreshold && c.Status == CredentialStatus.Active
            };
        }).ToList();

        return new CredentialHealthResult
        {
            Credentials = healthDtos,
            TotalActive = healthDtos.Count(c => c.Status == CredentialStatus.Active),
            ExpiringSoon = healthDtos.Count(c => c.IsExpiringSoon),
            Expired = healthDtos.Count(c => c.Status == CredentialStatus.Expired),
            Revoked = healthDtos.Count(c => c.Status == CredentialStatus.Revoked)
        };
    }

    public async Task<IReadOnlyList<OperationalAlertDto>> Handle(
        GetOperationalAlertsQuery request, CancellationToken cancellationToken)
    {
        List<OperationalAlertDto> alerts = [];
        DateTime now = DateTime.UtcNow;

        // 1. Credential expiration warnings
        int expiringCredentials = await dbContext.DoctorProviderCredentials
            .Where(c => c.Status == CredentialStatus.Active &&
                        c.ExpiresAt != null &&
                        c.ExpiresAt <= now.Add(CredentialExpirySoonThreshold))
            .CountAsync(cancellationToken);

        if (expiringCredentials > 0)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "CredentialExpiration",
                Severity = "Warning",
                Title = "Credentials Expiring Soon",
                Description = $"{expiringCredentials} doctor credential(s) will expire within {CredentialExpirySoonThreshold.Days} days.",
                DetectedAt = now
            });
        }

        int expiredCredentials = await dbContext.DoctorProviderCredentials
            .Where(c => c.Status == CredentialStatus.Expired)
            .CountAsync(cancellationToken);

        if (expiredCredentials > 0)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "CredentialExpired",
                Severity = "Critical",
                Title = "Expired Credentials",
                Description = $"{expiredCredentials} doctor credential(s) have expired and require renewal.",
                DetectedAt = now
            });
        }

        // 2. Provider outage detection (high recent transport/provider availability failure rate)
        DateTime recentWindow = now.AddHours(-1);
        var recentSubmissionCalls = dbContext.PrescriptionSubmissions
            .Where(s => s.SubmittedAt >= recentWindow)
            .Select(s => new
            {
                s.ProviderKey,
                IsFailure = s.CanonicalStatus == PrescriptionStatus.SyncFailed
            });

        var recentFailedPrescriptionCalls = dbContext.Prescriptions
            .Where(p => p.UpdatedAt >= recentWindow &&
                        p.SelectedProviderKey != null &&
                        (p.Status == PrescriptionStatus.SyncPending ||
                         p.Status == PrescriptionStatus.ProviderUnavailable))
            .Select(p => new
            {
                ProviderKey = p.SelectedProviderKey!,
                IsFailure = true
            });

        var recentProviderCalls = recentSubmissionCalls.Concat(recentFailedPrescriptionCalls);
        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            recentProviderCalls = recentProviderCalls.Where(c => c.ProviderKey == request.ProviderKey);
        }

        var recentOutages = await recentProviderCalls
            .GroupBy(c => c.ProviderKey)
            .Select(g => new
            {
                ProviderKey = g.Key,
                Total = g.Count(),
                Failures = g.Count(c => c.IsFailure)
            })
            .Where(x => x.Total >= 3 && (double)x.Failures / x.Total > 0.8)
            .ToListAsync(cancellationToken);

        foreach (var outage in recentOutages)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "ProviderOutage",
                Severity = "Critical",
                Title = $"Possible Provider Outage: {outage.ProviderKey}",
                Description = $"{outage.Failures}/{outage.Total} calls failed in the last hour ({(double)outage.Failures / outage.Total * 100:F0}% failure rate).",
                ProviderKey = outage.ProviderKey,
                DetectedAt = now
            });
        }

        // 4. Retry queue depth alerts
        string pendingStatus = "Pending";
        string processingStatus = "Processing";
        int queueDepth = await dbContext.OutboxRecords
            .Where(r => r.Status == pendingStatus || r.Status == processingStatus)
            .CountAsync(cancellationToken);

        if (queueDepth >= RetryQueueDepthCritical)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "RetryQueueDepth",
                Severity = "Critical",
                Title = "Retry Queue Critical",
                Description = $"Retry queue depth is {queueDepth} (threshold: {RetryQueueDepthCritical}). Immediate investigation required.",
                DetectedAt = now
            });
        }
        else if (queueDepth >= RetryQueueDepthWarning)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "RetryQueueDepth",
                Severity = "Warning",
                Title = "Retry Queue Growing",
                Description = $"Retry queue depth is {queueDepth} (warning threshold: {RetryQueueDepthWarning}).",
                DetectedAt = now
            });
        }

        // 5. Duplicate risk incidents pending
        int pendingDuplicateRisks = await dbContext.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.DuplicateRisk)
            .CountAsync(cancellationToken);

        if (pendingDuplicateRisks > 0)
        {
            alerts.Add(new OperationalAlertDto
            {
                AlertType = "DuplicateRiskPending",
                Severity = pendingDuplicateRisks > 5 ? "Critical" : "Warning",
                Title = "Duplicate Risk Incidents",
                Description = $"{pendingDuplicateRisks} prescription(s) flagged as duplicate risk require resolution.",
                DetectedAt = now
            });
        }

        return alerts.OrderByDescending(a => a.Severity == "Critical")
            .ThenByDescending(a => a.Severity == "Warning")
            .ToList();
    }

    public async Task<IReadOnlyList<StatusDistributionDto>> Handle(
        GetPrescriptionStatusDistributionQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionRecord> query = dbContext.Prescriptions.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKey);
        }

        if (request.From.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= request.To.Value);
        }

        int total = await query.CountAsync(cancellationToken);

        List<StatusDistributionDto> distribution = await query
            .GroupBy(p => new { p.Status, p.SelectedProviderKey })
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key.Status,
                ProviderKey = g.Key.SelectedProviderKey,
                Count = g.Count(),
                Percentage = total > 0 ? (double)g.Count() / total * 100 : 0
            })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);

        return distribution;
    }

    public async Task<DuplicateRiskIncidentsResult> Handle(
        GetDuplicateRiskIncidentsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionRecord> query = dbContext.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.DuplicateRisk ||
                        p.Status == PrescriptionStatus.ManuallyReconciled);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKey);
        }

        if (request.From.HasValue)
        {
            query = query.Where(p => p.UpdatedAt >= request.From.Value || p.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(p => p.UpdatedAt <= request.To.Value || p.CreatedAt <= request.To.Value);
        }

        List<DuplicateRiskIncidentDto> incidents = await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Take(request.Take)
            .Select(p => new DuplicateRiskIncidentDto
            {
                PrescriptionId = p.Id,
                ProviderKey = p.SelectedProviderKey,
                CurrentStatus = p.Status,
                DetectedAt = p.UpdatedAt ?? p.CreatedAt,
                ResolvedAt = p.Status == PrescriptionStatus.ManuallyReconciled ? p.UpdatedAt : null,
                ResolutionNotes = p.ReconciliationNotes
            })
            .ToListAsync(cancellationToken);

        int totalDuplicateRisk = await query
            .CountAsync(p => p.Status == PrescriptionStatus.DuplicateRisk, cancellationToken);

        int totalReconciled = await query
            .CountAsync(p => p.Status == PrescriptionStatus.ManuallyReconciled, cancellationToken);

        return new DuplicateRiskIncidentsResult
        {
            TotalIncidents = totalDuplicateRisk + totalReconciled,
            ResolvedCount = totalReconciled,
            PendingCount = totalDuplicateRisk,
            RecentIncidents = incidents
        };
    }

}
