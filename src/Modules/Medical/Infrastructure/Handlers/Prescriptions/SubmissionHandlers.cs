using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Platform.Retry;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Modules.Base.Core.Platform.Errors;
using Vitarish.Modules.Base.Core.Platform.Retry;

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
    ILogger<EditPrescriptionHandler> logger)
    : IRequestHandler<EditPrescriptionCommand, EditPrescriptionResult>
{
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

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            return EditPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
        {
            return EditPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);
        }

        // Transition to EditRequested → Submitting
        DateTime occurredAt = DateTime.UtcNow;
        prescription.TransitionTo(PrescriptionStatus.EditRequested, occurredAt);
        prescription.TransitionTo(PrescriptionStatus.Submitting, occurredAt);
        prescription.VersionNumber++;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        string idempotencyKey = $"{prescription.Id}:edit:v{prescription.VersionNumber}";

        AdapterResult<SubmissionResponse> editResult = await adapter.EditPrescriptionAsync(new EditRequest
        {
            PrescriptionId = prescription.Id,
            IdempotencyKey = idempotencyKey,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (editResult.IsSuccess && editResult.Data is not null)
        {
            PrescriptionStatus targetStatus = editResult.Data.CanonicalStatus;
            prescription.TransitionTo(targetStatus, DateTime.UtcNow);
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

        // On failure, stay in SyncPending
        prescription.TransitionTo(PrescriptionStatus.SyncPending, DateTime.UtcNow);
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
