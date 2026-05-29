using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

public sealed class SubmitPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    ILogger<SubmitPrescriptionHandler> logger)
    : IRequestHandler<SubmitPrescriptionCommand, SubmitPrescriptionResult>
{
    public async Task<SubmitPrescriptionResult> Handle(SubmitPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return SubmitPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);

        if (prescription.Status != PrescriptionStatus.ReadyForSubmission)
            return SubmitPrescriptionResult.Failure(
                "نسخه در وضعیت مناسب برای ارسال نیست. ابتدا اعتبارسنجی کنید.",
                prescription.Status);

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
            return SubmitPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);

        var adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
            return SubmitPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);

        // Transition to Submitting
        prescription.TransitionTo(PrescriptionStatus.Submitting);
        prescription.SubmittedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var idempotencyKey = $"{prescription.Id}:v{prescription.VersionNumber}";

        var submissionResult = await adapter.SubmitPrescriptionAsync(new SubmissionRequest
        {
            PrescriptionId = prescription.Id,
            IdempotencyKey = idempotencyKey,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (submissionResult.IsSuccess && submissionResult.Data is not null)
        {
            var targetStatus = submissionResult.Data.CanonicalStatus;
            prescription.TransitionTo(targetStatus);

            // Record submission
            var submission = new PrescriptionProviderSubmission
            {
                Id = Guid.NewGuid(),
                PrescriptionRecordId = prescription.Id,
                InsuranceProviderId = await GetProviderIdAsync(prescription.SelectedProviderKey, cancellationToken),
                OperationName = "SubmitPrescription",
                RemotePrescriptionId = submissionResult.Data.RemotePrescriptionId,
                RemoteTrackingCode = submissionResult.Data.RemoteTrackingCode,
                RemoteStatusCode = submissionResult.Data.RemoteStatusCode,
                CanonicalStatus = targetStatus,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            dbContext.PrescriptionSubmissions.Add(submission);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} submitted successfully to {Provider}, remote ID: {RemoteId}",
                prescription.Id, prescription.SelectedProviderKey, submissionResult.Data.RemotePrescriptionId);

            return SubmitPrescriptionResult.Success(
                submissionResult.Data.RemotePrescriptionId,
                submissionResult.Data.RemoteTrackingCode,
                targetStatus);
        }

        if (submissionResult.IsNotSupported)
        {
            prescription.TransitionTo(PrescriptionStatus.ProviderUnavailable);
            await dbContext.SaveChangesAsync(cancellationToken);
            return SubmitPrescriptionResult.Failure("بیمه‌گر از ثبت نسخه الکترونیک پشتیبانی نمی‌کند.", PrescriptionStatus.ProviderUnavailable);
        }

        // Handle failure
        var errorStatus = submissionResult.Error?.Category switch
        {
            Modules.Base.Platform.Errors.ErrorCategory.Timeout => PrescriptionStatus.SyncPending,
            Modules.Base.Platform.Errors.ErrorCategory.Outage => PrescriptionStatus.ProviderUnavailable,
            Modules.Base.Platform.Errors.ErrorCategory.DuplicateRisk => PrescriptionStatus.DuplicateRisk,
            _ => PrescriptionStatus.SyncPending
        };

        prescription.TransitionTo(errorStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Prescription {PrescriptionId} submission failed: {Error}",
            prescription.Id, submissionResult.Error?.UserMessage);

        return SubmitPrescriptionResult.Failure(
            submissionResult.Error?.UserMessage ?? "خطا در ارسال نسخه.",
            errorStatus);
    }

    private async Task<Guid> GetProviderIdAsync(string providerKey, CancellationToken cancellationToken)
    {
        var provider = await dbContext.InsuranceProviders
            .FirstOrDefaultAsync(p => p.ProviderKey == providerKey, cancellationToken);
        return provider?.Id ?? Guid.Empty;
    }
}

public sealed class EditPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    ILogger<EditPrescriptionHandler> logger)
    : IRequestHandler<EditPrescriptionCommand, EditPrescriptionResult>
{
    public async Task<EditPrescriptionResult> Handle(EditPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return EditPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);

        if (prescription.Status != PrescriptionStatus.AcceptedByProvider)
            return EditPrescriptionResult.Failure("فقط نسخه‌های تایید شده قابل ویرایش هستند.", prescription.Status);

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
            return EditPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);

        var adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
            return EditPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);

        // Transition to EditRequested → Submitting
        prescription.TransitionTo(PrescriptionStatus.EditRequested);
        prescription.TransitionTo(PrescriptionStatus.Submitting);
        prescription.VersionNumber++;
        await dbContext.SaveChangesAsync(cancellationToken);

        var idempotencyKey = $"{prescription.Id}:edit:v{prescription.VersionNumber}";

        var editResult = await adapter.EditPrescriptionAsync(new EditRequest
        {
            PrescriptionId = prescription.Id,
            IdempotencyKey = idempotencyKey,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (editResult.IsSuccess && editResult.Data is not null)
        {
            var targetStatus = editResult.Data.CanonicalStatus;
            prescription.TransitionTo(targetStatus);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} edited successfully", prescription.Id);

            return EditPrescriptionResult.Success(editResult.Data.RemoteTrackingCode, targetStatus);
        }

        // On failure, stay in Submitting (SyncPending)
        prescription.TransitionTo(PrescriptionStatus.SyncPending);
        await dbContext.SaveChangesAsync(cancellationToken);

        return EditPrescriptionResult.Failure(
            editResult.Error?.UserMessage ?? "خطا در ویرایش نسخه.",
            PrescriptionStatus.SyncPending);
    }
}

public sealed class CancelPrescriptionHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    ILogger<CancelPrescriptionHandler> logger)
    : IRequestHandler<CancelPrescriptionCommand, CancelPrescriptionResult>
{
    public async Task<CancelPrescriptionResult> Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return CancelPrescriptionResult.Failure("نسخه یافت نشد.", PrescriptionStatus.Draft);

        if (prescription.Status != PrescriptionStatus.AcceptedByProvider)
            return CancelPrescriptionResult.Failure("فقط نسخه‌های تایید شده قابل لغو هستند.", prescription.Status);

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
            return CancelPrescriptionResult.Failure("بیمه‌گر انتخاب نشده است.", prescription.Status);

        var adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
            return CancelPrescriptionResult.Failure("آداپتور بیمه‌گر در دسترس نیست.", prescription.Status);

        // Get the latest submission to find remote ID
        var submission = await dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission?.RemotePrescriptionId is null)
            return CancelPrescriptionResult.Failure("شناسه نسخه ریموت یافت نشد.", prescription.Status);

        // Transition to CancelRequested → Submitting
        prescription.TransitionTo(PrescriptionStatus.CancelRequested);
        prescription.TransitionTo(PrescriptionStatus.Submitting);
        await dbContext.SaveChangesAsync(cancellationToken);

        var cancelResult = await adapter.CancelPrescriptionAsync(new CancelRequest
        {
            RemotePrescriptionId = submission.RemotePrescriptionId,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (cancelResult.IsSuccess)
        {
            prescription.TransitionTo(PrescriptionStatus.Cancelled);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} cancelled successfully", prescription.Id);
            return CancelPrescriptionResult.Success(PrescriptionStatus.Cancelled);
        }

        // On failure
        prescription.TransitionTo(PrescriptionStatus.SyncPending);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CancelPrescriptionResult.Failure(
            cancelResult.Error?.UserMessage ?? "خطا در لغو نسخه.",
            PrescriptionStatus.SyncPending);
    }
}
