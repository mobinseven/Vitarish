using System.Text.Json;
using Vitarish.Modules.Base.Platform.Retry;

namespace Vitarish.Medical.Infrastructure.Services.Prescriptions;

/// <summary>
/// Prescription-scoped implementation of IOutboxService backed by the Prescription schema.
/// </summary>
internal sealed class PrescriptionOutboxService(PrescriptionDbContext dbContext) : IOutboxService
{
    public async Task<Guid> EnqueueAsync(OutboxEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        DateTime now = DateTime.UtcNow;

        PrescriptionOutboxRecord record = new()
        {
            Id = entry.Id,
            PrescriptionId = TryReadPrescriptionId(entry.PayloadJson),
            ProviderKey = entry.ProviderKey,
            OperationName = entry.OperationName,
            PayloadJson = entry.PayloadJson,
            IdempotencyKey = entry.IdempotencyKey,
            CorrelationId = entry.CorrelationId,
            Status = OutboxEntryStatus.Pending.ToString(),
            RetryCount = entry.RetryCount,
            MaxRetries = entry.MaxRetries,
            CreatedAt = now,
            NextRetryAt = entry.NextRetryAt ?? now,
            TenantId = entry.TenantId
        };

        _ = dbContext.OutboxRecords.Add(record);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    public async Task CompleteAsync(Guid entryId, CancellationToken cancellationToken = default) => await dbContext.OutboxRecords
            .Where(r => r.Id == entryId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.Status, OutboxEntryStatus.Completed.ToString()),
                cancellationToken);

    public async Task FailAsync(Guid entryId, string errorMessage, CancellationToken cancellationToken = default)
    {
        PrescriptionOutboxRecord? record = await dbContext.OutboxRecords
            .FirstOrDefaultAsync(r => r.Id == entryId, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.RetryCount++;
        record.LastError = errorMessage;
        DateTime now = DateTime.UtcNow;
        record.LastAttemptAt = now;

        if (record.RetryCount >= record.MaxRetries)
        {
            record.Status = OutboxEntryStatus.Failed.ToString();
        }
        else
        {
            record.Status = OutboxEntryStatus.Pending.ToString();
            // Exponential backoff: 1min, 2min, 4min, ...
            double backoffMinutes = Math.Pow(2, record.RetryCount - 1);
            record.NextRetryAt = now.AddMinutes(backoffMinutes);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDuplicateRiskAsync(Guid entryId, string reason, CancellationToken cancellationToken = default) => await dbContext.OutboxRecords
            .Where(r => r.Id == entryId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.Status, OutboxEntryStatus.DuplicateRisk.ToString())
                      .SetProperty(r => r.LastError, reason),
                cancellationToken);

    public async Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(
        string? providerKey = null,
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        string pendingStatus = OutboxEntryStatus.Pending.ToString();
        DateTime now = DateTime.UtcNow;

        IQueryable<PrescriptionOutboxRecord> query = dbContext.OutboxRecords
            .Where(r => r.Status == pendingStatus && r.NextRetryAt <= now);

        if (!string.IsNullOrEmpty(providerKey))
        {
            query = query.Where(r => r.ProviderKey == providerKey);
        }

        List<PrescriptionOutboxRecord> records = await query
            .OrderBy(r => r.NextRetryAt)
            .Take(maxItems)
            .ToListAsync(cancellationToken);

        return records.Select(r => new OutboxEntry
        {
            Id = r.Id,
            ProviderKey = r.ProviderKey,
            OperationName = r.OperationName,
            PayloadJson = r.PayloadJson,
            IdempotencyKey = r.IdempotencyKey,
            CorrelationId = r.CorrelationId,
            Status = Enum.TryParse<OutboxEntryStatus>(r.Status, out OutboxEntryStatus s) ? s : OutboxEntryStatus.Pending,
            RetryCount = r.RetryCount,
            MaxRetries = r.MaxRetries,
            CreatedAt = r.CreatedAt,
            NextRetryAt = r.NextRetryAt,
            LastAttemptAt = r.LastAttemptAt,
            LastError = r.LastError,
            TenantId = r.TenantId
        }).ToList();
    }

    private static Guid? TryReadPrescriptionId(string payloadJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.TryGetProperty("PrescriptionId", out JsonElement idElement) &&
                idElement.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idElement.GetString(), out Guid prescriptionId))
            {
                return prescriptionId;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
