using System.Text.Json;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Modules.Base.Core.Platform.Errors;
using Vitarish.Modules.Base.Core.Platform.Retry;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class SubmitPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IOutboxService outboxService,
    IAuditWriter auditWriter,
    ILogger<SubmitPrescriptionHandler> logger)
    : IRequestHandler<SubmitPrescriptionCommand, SubmitPrescriptionResult>
{
    public async Task<SubmitPrescriptionResult> Handle(SubmitPrescriptionCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return SubmitPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);
        }

        if (prescription.Status != PrescriptionStatus.ReadyForSubmission)
        {
            return SubmitPrescriptionResult.Failure(
                "نسخه در وضعیت مناسب برای ارسال نیست. ابتدا اعتبارسنجی کنید.",
                prescription.Status);
        }

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            return SubmitPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
        {
            return SubmitPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);
        }

        // Transition to Submitting
        DateTime submittedAt = DateTime.UtcNow;
        prescription.TransitionTo(PrescriptionStatus.Submitting, submittedAt);
        prescription.SubmittedAt = submittedAt;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        string idempotencyKey = $"{prescription.Id}:v{prescription.VersionNumber}";

        AdapterResult<SubmissionResponse> submissionResult = await adapter.SubmitPrescriptionAsync(new SubmissionRequest
        {
            PrescriptionId = prescription.Id,
            IdempotencyKey = idempotencyKey,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (submissionResult.IsSuccess && submissionResult.Data is not null)
        {
            PrescriptionStatus targetStatus = submissionResult.Data.CanonicalStatus;
            prescription.TransitionTo(targetStatus, DateTime.UtcNow);

            // Record submission
            PrescriptionProviderSubmission submission = new()
            {
                Id = Guid.NewGuid(),
                PrescriptionRecordId = prescription.Id,
                ProviderKey = prescription.SelectedProviderKey!,
                OperationName = "SubmitPrescription",
                RemotePrescriptionId = submissionResult.Data.RemotePrescriptionId,
                RemoteTrackingCode = submissionResult.Data.RemoteTrackingCode,
                RemoteStatusCode = submissionResult.Data.RemoteStatusCode,
                CanonicalStatus = targetStatus,
                SubmittedAt = submittedAt
            };

            _ = dbContext.PrescriptionSubmissions.Add(submission);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(PrescriptionRecord),
                EntityId = prescription.Id,
                Action = "Submitted",
                OldState = PrescriptionStatus.Submitting.ToString(),
                NewState = targetStatus.ToString(),
                CorrelationId = request.CorrelationId,
                ProviderKey = prescription.SelectedProviderKey,
                OccurredAt = submittedAt,
                TenantId = prescription.TenantId
            }, cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} submitted successfully to {Provider}, remote ID: {RemoteId}",
                prescription.Id, prescription.SelectedProviderKey, submissionResult.Data.RemotePrescriptionId);

            return SubmitPrescriptionResult.Success(
                submissionResult.Data.RemotePrescriptionId,
                submissionResult.Data.RemoteTrackingCode,
                targetStatus);
        }

        if (submissionResult.IsNotSupported)
        {
            prescription.TransitionTo(PrescriptionStatus.ProviderUnavailable, DateTime.UtcNow);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return SubmitPrescriptionResult.Failure("بیمه‌گر از ثبت نسخه الکترونیک پشتیبانی نمی‌کند.", PrescriptionStatus.ProviderUnavailable);
        }

        // Handle failure
        PrescriptionStatus errorStatus = submissionResult.Error?.Category switch
        {
            ErrorCategory.Timeout => PrescriptionStatus.SyncPending,
            ErrorCategory.Outage => PrescriptionStatus.ProviderUnavailable,
            ErrorCategory.DuplicateRisk => PrescriptionStatus.DuplicateRisk,
            _ => PrescriptionStatus.SyncPending
        };

        prescription.TransitionTo(errorStatus, DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        // Enqueue for retry if SyncPending
        if (errorStatus == PrescriptionStatus.SyncPending)
        {
            _ = await outboxService.EnqueueAsync(new OutboxEntry
            {
                ProviderKey = prescription.SelectedProviderKey,
                OperationName = "SubmitPrescription",
                PayloadJson = JsonSerializer.Serialize(new { PrescriptionId = prescription.Id, request.CorrelationId }),
                IdempotencyKey = idempotencyKey,
                CorrelationId = request.CorrelationId,
                NextRetryAt = DateTime.UtcNow.AddMinutes(1),
                TenantId = prescription.TenantId
            }, cancellationToken);
        }

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "SubmissionFailed",
            OldState = PrescriptionStatus.Submitting.ToString(),
            NewState = errorStatus.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogWarning("Prescription {PrescriptionId} submission failed: {Error}",
            prescription.Id, submissionResult.Error?.UserMessage);

        return SubmitPrescriptionResult.Failure(
            submissionResult.Error?.UserMessage ?? "خطا در ارسال نسخه.",
            errorStatus);
    }
}

public sealed class EditPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    IRequestContext requestContext,
    ILogger<EditPrescriptionHandler> logger)
    : IRequestHandler<EditPrescriptionCommand, EditPrescriptionResult>
{
    private const int MaxEditItems = 100;
    private const int MaxReasonLength = 1024;
    private const int MaxReferenceCodeLength = 128;
    private const int MaxDisplayNameLength = 512;
    private const int MaxJsonLength = 4096;

    public async Task<EditPrescriptionResult> Handle(EditPrescriptionCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return EditPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);
        }

        if (prescription.Status != PrescriptionStatus.AcceptedByProvider)
        {
            return EditPrescriptionResult.Failure("فقط نسخه‌های تایید شده قابل ویرایش هستند.", prescription.Status);
        }

        if (!Guid.TryParse(requestContext.OwnerId, out Guid actorId) || actorId != prescription.DoctorId ||
            !string.Equals(requestContext.TenantId, prescription.TenantId, StringComparison.Ordinal))
        {
            return EditPrescriptionResult.Failure("شما مجاز به ویرایش این نسخه نیستید.", prescription.Status);
        }

        if (request.ExpectedVersion != prescription.VersionNumber)
        {
            return EditPrescriptionResult.Failure("نسخه توسط شخص دیگری تغییر کرده است. لطفاً صفحه را بازخوانی کنید.", prescription.Status);
        }

        if (!IsValidEditRequest(request))
        {
            return EditPrescriptionResult.Failure("اطلاعات ویرایش نسخه نامعتبر یا بیش از حد مجاز است.", prescription.Status);
        }

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            return EditPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
        {
            return EditPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);
        }

        if (!string.Equals(adapter.ProviderKey, "tamin", StringComparison.OrdinalIgnoreCase) ||
            !adapter.GetCapabilities().Contains(InsuranceProviderCapabilityType.SupportsEditPrescription))
        {
            return EditPrescriptionResult.Failure("این بیمه‌گر از ویرایش نسخه پشتیبانی نمی‌کند.", prescription.Status);
        }

        string previousItems = JsonSerializer.Serialize(prescription.Items);
        string proposedItems = JsonSerializer.Serialize(request.Items);
        PrescriptionEditAudit audit = new()
        {
            Id = Guid.NewGuid(), PrescriptionRecordId = prescription.Id, PreviousVersion = prescription.VersionNumber,
            Reason = request.EditReason, PreviousItemsJson = previousItems, ProposedItemsJson = proposedItems,
            TenantId = prescription.TenantId, CreatedAt = DateTime.UtcNow
        };
        _ = dbContext.PrescriptionEditAudits.Add(audit);
        dbContext.PrescriptionItems.RemoveRange(prescription.Items);
        prescription.Items = request.Items.Select((item, index) => new PrescriptionItem
        {
            Id = Guid.NewGuid(), PrescriptionRecordId = prescription.Id, Category = item.Category,
            ReferenceCode = item.ReferenceCode, DisplayName = item.DisplayName, DosageJson = item.DosageJson,
            Quantity = item.Quantity, DurationDays = item.DurationDays, MetadataJson = item.MetadataJson,
            SortOrder = index + 1, CreatedAt = DateTime.UtcNow
        }).ToList();

        // Transition to EditRequested → Submitting
        DateTime occurredAt = DateTime.UtcNow;
        prescription.TransitionTo(PrescriptionStatus.EditRequested, occurredAt);
        prescription.TransitionTo(PrescriptionStatus.Submitting, occurredAt);
        prescription.VersionNumber++;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        string idempotencyKey = $"{prescription.Id}:edit:v{prescription.VersionNumber}";

        AdapterResult<SubmissionResponse> editResult;
        try
        {
            editResult = await adapter.EditPrescriptionAsync(new EditRequest
            {
                PrescriptionId = prescription.Id,
                IdempotencyKey = idempotencyKey,
                CorrelationId = request.CorrelationId
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkEditOutcomeUnknownAsync(
                dbContext,
                prescription,
                audit,
                logger,
                "Prescription amendment was canceled after the provider mutation started.");
            throw;
        }
        catch (Exception ex)
        {
            await MarkEditOutcomeUnknownAsync(
                dbContext,
                prescription,
                audit,
                logger,
                "Prescription amendment failed with an unknown provider outcome.",
                ex);
            return EditPrescriptionResult.Failure(
                "نتیجه ویرایش نسخه نامشخص است و برای تطبیق ثبت شد.",
                PrescriptionStatus.SyncPending);
        }

        if (editResult.IsSuccess && editResult.Data is not null)
        {
            PrescriptionStatus targetStatus = editResult.Data.CanonicalStatus;
            prescription.TransitionTo(targetStatus, DateTime.UtcNow);
            audit.Outcome = "Accepted";
            _ = dbContext.PrescriptionSubmissions.Add(new PrescriptionProviderSubmission
            {
                Id = Guid.NewGuid(), PrescriptionRecordId = prescription.Id, ProviderKey = prescription.SelectedProviderKey!,
                OperationName = "EditPrescription", RemotePrescriptionId = editResult.Data.RemotePrescriptionId,
                RemoteTrackingCode = editResult.Data.RemoteTrackingCode, RemoteStatusCode = editResult.Data.RemoteStatusCode,
                CanonicalStatus = targetStatus, SubmissionVersion = prescription.VersionNumber, SubmittedAt = occurredAt
            });
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(PrescriptionRecord),
                EntityId = prescription.Id,
                Action = "Edited",
                OldState = PrescriptionStatus.Submitting.ToString(),
                NewState = targetStatus.ToString(),
                CorrelationId = request.CorrelationId,
                ProviderKey = prescription.SelectedProviderKey,
                OccurredAt = DateTime.UtcNow,
                TenantId = prescription.TenantId
            }, cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} edited successfully", prescription.Id);

            return EditPrescriptionResult.Success(editResult.Data.RemoteTrackingCode, targetStatus);
        }

        if (editResult.Error?.Category is ErrorCategory.Validation or ErrorCategory.BusinessRule)
        {
            List<PrescriptionItem>? original = JsonSerializer.Deserialize<List<PrescriptionItem>>(previousItems);
            dbContext.PrescriptionItems.RemoveRange(prescription.Items);
            prescription.Items = original ?? [];
            foreach (PrescriptionItem item in prescription.Items) { item.Id = Guid.NewGuid(); item.PrescriptionRecordId = prescription.Id; }
            prescription.TransitionTo(PrescriptionStatus.AcceptedByProvider, DateTime.UtcNow);
            audit.Outcome = "RejectedRestored";
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return EditPrescriptionResult.Failure(editResult.Error?.UserMessage ?? "ویرایش نسخه رد شد.", prescription.Status);
        }

        // Technical uncertainty retains the proposed items for reconciliation.
        prescription.TransitionTo(PrescriptionStatus.SyncPending, DateTime.UtcNow);
        audit.Outcome = "SyncPending";
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "EditFailed",
            OldState = PrescriptionStatus.Submitting.ToString(),
            NewState = PrescriptionStatus.SyncPending.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        return EditPrescriptionResult.Failure(
            editResult.Error?.UserMessage ?? "خطا در ویرایش نسخه.",
            PrescriptionStatus.SyncPending);
    }

    private static async Task MarkEditOutcomeUnknownAsync(
        PrescriptionDbContext dbContext,
        PrescriptionRecord prescription,
        PrescriptionEditAudit audit,
        ILogger logger,
        string message,
        Exception? exception = null)
    {
        prescription.TransitionTo(PrescriptionStatus.SyncPending, DateTime.UtcNow);
        audit.Outcome = "SyncPending";
        await dbContext.SaveChangesAsync(CancellationToken.None);
        logger.LogWarning(
            exception,
            "{Message} Prescription {PrescriptionId} requires reconciliation.",
            message,
            prescription.Id);
    }

    internal static bool IsValidEditRequest(EditPrescriptionCommand request)
    {
        if (request.Items is not { Count: > 0 and <= MaxEditItems } ||
            request.EditReason?.Length > MaxReasonLength)
        {
            return false;
        }

        return request.Items.All(item =>
            item.Quantity >= 1 &&
            item.DurationDays is null or > 0 &&
            item.ReferenceCode is null or { Length: <= MaxReferenceCodeLength } &&
            item.DisplayName is null or { Length: <= MaxDisplayNameLength } &&
            IsValidJson(item.DosageJson) &&
            IsValidJson(item.MetadataJson));
    }

    private static bool IsValidJson(string? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value.Length > MaxJsonLength)
        {
            return false;
        }

        try
        {
            using JsonDocument _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class CancelPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    ILogger<CancelPrescriptionHandler> logger)
    : IRequestHandler<CancelPrescriptionCommand, CancelPrescriptionResult>
{
    public async Task<CancelPrescriptionResult> Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return CancelPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);
        }

        if (prescription.Status != PrescriptionStatus.AcceptedByProvider)
        {
            return CancelPrescriptionResult.Failure("فقط نسخه‌های تایید شده قابل لغو هستند.", prescription.Status);
        }

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            return CancelPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
        {
            return CancelPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);
        }

        // Get the latest submission to find remote ID
        PrescriptionProviderSubmission? submission = await dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission?.RemotePrescriptionId is null)
        {
            return CancelPrescriptionResult.Failure("شناسه نسخه ریموت یافت نشد.", prescription.Status);
        }

        // Transition to CancelRequested → Submitting
        prescription.TransitionTo(PrescriptionStatus.CancelRequested, DateTime.UtcNow);
        prescription.CancellationReason = request.Reason;
        prescription.TransitionTo(PrescriptionStatus.Submitting, DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        AdapterResult cancelResult = await adapter.CancelPrescriptionAsync(new CancelRequest
        {
            RemotePrescriptionId = submission.RemotePrescriptionId,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (cancelResult.IsSuccess)
        {
            prescription.TransitionTo(PrescriptionStatus.Cancelled, DateTime.UtcNow);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(PrescriptionRecord),
                EntityId = prescription.Id,
                Action = "Cancelled",
                OldState = PrescriptionStatus.Submitting.ToString(),
                NewState = PrescriptionStatus.Cancelled.ToString(),
                CorrelationId = request.CorrelationId,
                ProviderKey = prescription.SelectedProviderKey,
                OccurredAt = DateTime.UtcNow,
                TenantId = prescription.TenantId
            }, cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} cancelled successfully", prescription.Id);
            return CancelPrescriptionResult.Success(PrescriptionStatus.Cancelled);
        }

        // On failure
        prescription.TransitionTo(PrescriptionStatus.SyncPending, DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "CancelFailed",
            OldState = PrescriptionStatus.Submitting.ToString(),
            NewState = PrescriptionStatus.SyncPending.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        return CancelPrescriptionResult.Failure(
            cancelResult.Error?.UserMessage ?? "خطا در لغو نسخه.",
            PrescriptionStatus.SyncPending);
    }
}
