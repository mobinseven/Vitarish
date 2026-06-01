using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Prescription.Application.Queries;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

internal sealed class ObservabilityQueryHandlers(PrescriptionDbContext dbContext) :
    IRequestHandler<GetProviderTransactionMetricsQuery, IReadOnlyList<ProviderTransactionMetricsDto>>,
    IRequestHandler<GetRejectionDistributionQuery, IReadOnlyList<RejectionDistributionDto>>,
    IRequestHandler<GetCredentialHealthQuery, CredentialHealthResult>,
    IRequestHandler<GetOperationalAlertsQuery, IReadOnlyList<OperationalAlertDto>>,
    IRequestHandler<GetPrescriptionStatusDistributionQuery, IReadOnlyList<StatusDistributionDto>>,
    IRequestHandler<GetDuplicateRiskIncidentsQuery, DuplicateRiskIncidentsResult>
{
    private static readonly TimeSpan CredentialExpirySoonThreshold = TimeSpan.FromDays(7);
    private static readonly TimeSpan ReferenceDataStaleThreshold = TimeSpan.FromDays(7);
    private static readonly int RetryQueueDepthWarning = 10;
    private static readonly int RetryQueueDepthCritical = 50;

    public async Task<IReadOnlyList<ProviderTransactionMetricsDto>> Handle(
        GetProviderTransactionMetricsQuery request, CancellationToken cancellationToken)
    {
        // Derive transaction metrics from PrescriptionProviderSubmission records
        var query = dbContext.PrescriptionSubmissions.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var providerIds = await dbContext.InsuranceProviders
                .Where(p => p.ProviderKey == request.ProviderKey)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(s => providerIds.Contains(s.InsuranceProviderId));
        }

        if (request.From.HasValue)
            query = query.Where(s => s.SubmittedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(s => s.SubmittedAt <= request.To.Value);

        var submissions = await query.ToListAsync(cancellationToken);

        var providerKeyLookup = await dbContext.InsuranceProviders
            .ToDictionaryAsync(p => p.Id, p => p.ProviderKey, cancellationToken);

        var metrics = submissions
            .GroupBy(s => new { ProviderKey = providerKeyLookup.GetValueOrDefault(s.InsuranceProviderId, "Unknown"), s.OperationName })
            .Select(g =>
            {
                var successCount = g.Count(s => s.CanonicalStatus == PrescriptionStatus.AcceptedByProvider ||
                                               s.CanonicalStatus == PrescriptionStatus.Submitted);
                var failureCount = g.Count(s => s.CanonicalStatus == PrescriptionStatus.RejectedByProvider ||
                                               s.CanonicalStatus == PrescriptionStatus.SyncFailed);
                var total = g.Count();

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
        var query = dbContext.PrescriptionSubmissions
            .Where(s => s.CanonicalStatus == PrescriptionStatus.RejectedByProvider);

        if (request.From.HasValue)
            query = query.Where(s => s.SubmittedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(s => s.SubmittedAt <= request.To.Value);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var providerIds = await dbContext.InsuranceProviders
                .Where(p => p.ProviderKey == request.ProviderKey)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(r => providerIds.Contains(r.InsuranceProviderId));
        }

        var rejections = await query.ToListAsync(cancellationToken);
        var totalRejections = rejections.Count;

        var providerKeyLookup = await dbContext.InsuranceProviders
            .ToDictionaryAsync(p => p.Id, p => p.ProviderKey, cancellationToken);

        var distribution = rejections
            .GroupBy(r => new { ProviderKey = providerKeyLookup.GetValueOrDefault(r.InsuranceProviderId, "Unknown"), ErrorCategory = r.RemoteStatusCode ?? "Unknown" })
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
        var query = dbContext.DoctorProviderCredentials.AsQueryable();

        if (request.ClinicId.HasValue)
            query = query.Where(c => c.ClinicId == request.ClinicId.Value);

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            var providerIds = await dbContext.InsuranceProviders
                .Where(p => p.ProviderKey == request.ProviderKey)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(c => providerIds.Contains(c.InsuranceProviderId));
        }

        var credentials = await query.ToListAsync(cancellationToken);
        var providers = await dbContext.InsuranceProviders
            .ToDictionaryAsync(p => p.Id, p => p.ProviderKey, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var soonThreshold = now.Add(CredentialExpirySoonThreshold);

        var healthDtos = credentials.Select(c =>
        {
            var daysUntilExpiry = c.ExpiresAt.HasValue
                ? (int)(c.ExpiresAt.Value - now).TotalDays
                : int.MaxValue;

            return new CredentialHealthDto
            {
                CredentialId = c.Id,
                DoctorId = c.DoctorId,
                ClinicId = c.ClinicId,
                ProviderKey = providers.GetValueOrDefault(c.InsuranceProviderId, "Unknown"),
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
        var alerts = new List<OperationalAlertDto>();
        var now = DateTimeOffset.UtcNow;

        // 1. Credential expiration warnings
        var expiringCredentials = await dbContext.DoctorProviderCredentials
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

        var expiredCredentials = await dbContext.DoctorProviderCredentials
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

        // 2. Reference data staleness alerts
        var staleThreshold = now - ReferenceDataStaleThreshold;
        var staleProviders = await dbContext.ReferenceCodeSyncJobs
            .GroupBy(j => new { j.ProviderKey, j.CodeSystem })
            .Select(g => new
            {
                g.Key.ProviderKey,
                g.Key.CodeSystem,
                LastSuccess = g
                    .Where(j => j.Status == ReferenceCodeSyncStatus.Completed)
                    .Max(j => (DateTimeOffset?)j.CompletedAt)
            })
            .Where(x => x.LastSuccess == null || x.LastSuccess < staleThreshold)
            .ToListAsync(cancellationToken);

        foreach (var stale in staleProviders)
        {
            if (!string.IsNullOrEmpty(request.ProviderKey) && stale.ProviderKey != request.ProviderKey)
                continue;

            alerts.Add(new OperationalAlertDto
            {
                AlertType = "ReferenceDataStale",
                Severity = "Warning",
                Title = $"Stale Reference Data: {stale.ProviderKey}/{stale.CodeSystem}",
                Description = stale.LastSuccess.HasValue
                    ? $"Last successful sync was {(now - stale.LastSuccess.Value).Days} days ago."
                    : "No successful sync recorded.",
                ProviderKey = stale.ProviderKey,
                DetectedAt = now
            });
        }

        // 3. Provider outage detection (high recent transport/provider availability failure rate)
        var recentWindow = now.AddHours(-1);
        var recentSubmissionCalls = dbContext.PrescriptionSubmissions
            .Where(s => s.SubmittedAt >= recentWindow)
            .Join(dbContext.InsuranceProviders,
                s => s.InsuranceProviderId,
                p => p.Id,
                (s, p) => new
                {
                    p.ProviderKey,
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
            recentProviderCalls = recentProviderCalls.Where(c => c.ProviderKey == request.ProviderKey);

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
        var pendingStatus = "Pending";
        var processingStatus = "Processing";
        var queueDepth = await dbContext.OutboxRecords
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
        var pendingDuplicateRisks = await dbContext.Prescriptions
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
        var query = dbContext.Prescriptions.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKey);
        if (request.From.HasValue)
            query = query.Where(p => p.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(p => p.CreatedAt <= request.To.Value);

        var total = await query.CountAsync(cancellationToken);

        var distribution = await query
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
        var query = dbContext.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.DuplicateRisk ||
                        p.Status == PrescriptionStatus.ManuallyReconciled);

        if (!string.IsNullOrEmpty(request.ProviderKey))
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKey);
        if (request.From.HasValue)
            query = query.Where(p => p.UpdatedAt >= request.From.Value || p.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(p => p.UpdatedAt <= request.To.Value || p.CreatedAt <= request.To.Value);

        var incidents = await query
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

        var totalDuplicateRisk = await query
            .CountAsync(p => p.Status == PrescriptionStatus.DuplicateRisk, cancellationToken);

        var totalReconciled = await query
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
