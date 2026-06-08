using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base.Platform.Retry;
using Vitarish.Medical.Application.Queries;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Enums;
using Vitarish.Medical.Infrastructure.Data;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class GetReconciliationQueueHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReconciliationQueueQuery, ReconciliationQueueResult>
{
    private static readonly PrescriptionStatus[] ReconciliationStatuses =
    [
        PrescriptionStatus.SyncPending,
        PrescriptionStatus.SyncFailed,
        PrescriptionStatus.DuplicateRisk,
        PrescriptionStatus.PaperFallback,
        PrescriptionStatus.ProviderUnavailable
    ];

    public async Task<ReconciliationQueueResult> Handle(GetReconciliationQueueQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionRecord> query = dbContext.Prescriptions
            .IgnoreQueryFilters()
            .Where(p => p.DeletedAt == null)
            .Where(p => ReconciliationStatuses.Contains(p.Status));

        if (!string.IsNullOrEmpty(request.TenantId))
        {
            query = query.Where(p => p.TenantId == request.TenantId);
        }

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(p => p.SelectedProviderKey == request.ProviderKey);
        }

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(p => p.Status == request.StatusFilter.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<ReconciliationItemDto> items = await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(p => new ReconciliationItemDto
            {
                PrescriptionId = p.Id,
                PatientId = p.PatientId,
                PatientNationalId = p.PatientNationalId,
                DoctorId = p.DoctorId,
                ClinicId = p.ClinicId,
                Status = p.Status,
                SelectedProviderKey = p.SelectedProviderKey,
                CreatedAt = p.CreatedAt,
                SubmittedAt = p.SubmittedAt,
                UpdatedAt = p.UpdatedAt,
                PaperTrackingNumber = p.PaperTrackingNumber,
                RetryCount = dbContext.OutboxRecords
                    .Where(o => o.PrescriptionId == p.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.RetryCount)
                    .FirstOrDefault(),
                LastError = dbContext.OutboxRecords
                    .Where(o => o.PrescriptionId == p.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.LastError)
                    .FirstOrDefault(),
                RemotePrescriptionId = dbContext.PrescriptionSubmissions
                    .Where(s => s.PrescriptionRecordId == p.Id)
                    .OrderByDescending(s => s.SubmittedAt)
                    .Select(s => s.RemotePrescriptionId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new ReconciliationQueueResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}

public sealed class GetOutboxStatusHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetOutboxStatusQuery, OutboxStatusResult>
{
    public async Task<OutboxStatusResult> Handle(GetOutboxStatusQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionOutboxRecord> query = dbContext.OutboxRecords.AsQueryable();

        if (!string.IsNullOrEmpty(request.ProviderKey))
        {
            query = query.Where(r => r.ProviderKey == request.ProviderKey);
        }

        if (!string.IsNullOrEmpty(request.TenantId))
        {
            query = query.Where(r => r.TenantId == request.TenantId);
        }

        int pendingCount = await query.CountAsync(r => r.Status == OutboxEntryStatus.Pending.ToString(), cancellationToken);
        int processingCount = await query.CountAsync(r => r.Status == OutboxEntryStatus.Processing.ToString(), cancellationToken);
        int failedCount = await query.CountAsync(r => r.Status == OutboxEntryStatus.Failed.ToString(), cancellationToken);
        int duplicateRiskCount = await query.CountAsync(r => r.Status == OutboxEntryStatus.DuplicateRisk.ToString(), cancellationToken);

        List<OutboxEntryDto> recentEntries = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .Select(r => new OutboxEntryDto
            {
                Id = r.Id,
                PrescriptionId = r.PrescriptionId,
                ProviderKey = r.ProviderKey,
                OperationName = r.OperationName,
                Status = r.Status,
                RetryCount = r.RetryCount,
                MaxRetries = r.MaxRetries,
                CreatedAt = r.CreatedAt,
                NextRetryAt = r.NextRetryAt,
                LastAttemptAt = r.LastAttemptAt,
                LastError = r.LastError
            })
            .ToListAsync(cancellationToken);

        return new OutboxStatusResult
        {
            PendingCount = pendingCount,
            ProcessingCount = processingCount,
            FailedCount = failedCount,
            DuplicateRiskCount = duplicateRiskCount,
            RecentEntries = recentEntries
        };
    }
}
