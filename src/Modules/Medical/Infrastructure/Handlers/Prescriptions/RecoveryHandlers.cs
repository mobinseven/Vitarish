using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Application.Commands;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Enums;
using Vitarish.Medical.Infrastructure.Data;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class PaperFallbackHandler(
    MedicalDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<PaperFallbackHandler> logger)
    : IRequestHandler<PaperFallbackCommand, PaperFallbackResult>
{
    public async Task<PaperFallbackResult> Handle(PaperFallbackCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PaperFallbackResult.Failure("نسخه یافت نشد.");
        }

        if (!PrescriptionRecord.IsValidTransition(prescription.Status, PrescriptionStatus.PaperFallback))
        {
            return PaperFallbackResult.Failure($"انتقال از وضعیت '{prescription.Status}' به 'کاغذی' مجاز نیست.");
        }

        if (request.Reason is not null && request.Reason.Length > 1024)
        {
            return PaperFallbackResult.Failure("دلیل بازگشت به نسخه کاغذی نمی‌تواند بیش از ۱۰۲۴ کاراکتر باشد.");
        }

        if (request.PaperTrackingNumber is not null && request.PaperTrackingNumber.Length > 256)
        {
            return PaperFallbackResult.Failure("شماره رهگیری کاغذی نمی‌تواند بیش از ۲۵۶ کاراکتر باشد.");
        }

        PrescriptionStatus previousStatus = prescription.Status;
        prescription.TransitionTo(PrescriptionStatus.PaperFallback);
        prescription.FallbackReason = request.Reason;
        prescription.PaperTrackingNumber = request.PaperTrackingNumber;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "PaperFallback",
            OldState = previousStatus.ToString(),
            NewState = PrescriptionStatus.PaperFallback.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogInformation("Prescription {PrescriptionId} moved to PaperFallback. Reason: {Reason}",
            prescription.Id, request.Reason);

        return PaperFallbackResult.Success();
    }
}

public sealed class ResolveDuplicateRiskHandler(
    MedicalDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<ResolveDuplicateRiskHandler> logger)
    : IRequestHandler<ResolveDuplicateRiskCommand, ResolveDuplicateRiskResult>
{
    public async Task<ResolveDuplicateRiskResult> Handle(ResolveDuplicateRiskCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return ResolveDuplicateRiskResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status != PrescriptionStatus.DuplicateRisk)
        {
            return ResolveDuplicateRiskResult.Failure("نسخه در وضعیت 'ریسک تکراری' نیست.");
        }

        PrescriptionStatus targetStatus = request.Resolution switch
        {
            DuplicateRiskResolution.ConfirmedAccepted => PrescriptionStatus.AcceptedByProvider,
            DuplicateRiskResolution.ConfirmedNotSubmitted => PrescriptionStatus.ManuallyReconciled,
            _ => PrescriptionStatus.ManuallyReconciled
        };

        prescription.TransitionTo(targetStatus);
        prescription.ReconciliationNotes = request.Notes;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "DuplicateRiskResolved",
            OldState = PrescriptionStatus.DuplicateRisk.ToString(),
            NewState = targetStatus.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogInformation("Prescription {PrescriptionId} DuplicateRisk resolved: {Resolution} → {Status}",
            prescription.Id, request.Resolution, targetStatus);

        return ResolveDuplicateRiskResult.Success(targetStatus);
    }
}

public sealed class ManualReconcileHandler(
    MedicalDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<ManualReconcileHandler> logger)
    : IRequestHandler<ManualReconcileCommand, ManualReconcileResult>
{
    public async Task<ManualReconcileResult> Handle(ManualReconcileCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return ManualReconcileResult.Failure("نسخه یافت نشد.");
        }

        if (!PrescriptionRecord.IsValidTransition(prescription.Status, PrescriptionStatus.ManuallyReconciled))
        {
            return ManualReconcileResult.Failure($"انتقال از وضعیت '{prescription.Status}' به 'تسویه دستی' مجاز نیست.");
        }

        PrescriptionStatus previousStatus = prescription.Status;
        prescription.TransitionTo(PrescriptionStatus.ManuallyReconciled);
        prescription.ReconciliationNotes = request.Notes;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "ManuallyReconciled",
            OldState = previousStatus.ToString(),
            NewState = PrescriptionStatus.ManuallyReconciled.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogInformation("Prescription {PrescriptionId} manually reconciled from {PreviousStatus}",
            prescription.Id, previousStatus);

        return ManualReconcileResult.Success();
    }
}

public sealed class RefreshProviderStatusHandler(
    MedicalDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    ILogger<RefreshProviderStatusHandler> logger)
    : IRequestHandler<RefreshProviderStatusCommand, RefreshProviderStatusResult>
{
    public async Task<RefreshProviderStatusResult> Handle(RefreshProviderStatusCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return RefreshProviderStatusResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status is not (PrescriptionStatus.SyncPending or PrescriptionStatus.DuplicateRisk))
        {
            return RefreshProviderStatusResult.Failure("بررسی وضعیت فقط برای نسخه‌های معلق یا ریسک تکراری امکان‌پذیر است.");
        }

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            return RefreshProviderStatusResult.Failure("بیمه‌گر انتخاب نشده است.");
        }

        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null)
        {
            return RefreshProviderStatusResult.Failure("آداپتور بیمه‌گر در دسترس نیست.");
        }

        // Find the last submission's remote ID
        PrescriptionProviderSubmission? submission = await dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        AdapterResult<FetchResponse> fetchResult = await adapter.FetchPrescriptionAsync(new FetchRequest
        {
            RemotePrescriptionId = submission?.RemotePrescriptionId,
            CorrelationId = request.CorrelationId
        }, cancellationToken);

        if (!fetchResult.IsSuccess || fetchResult.Data is null)
        {
            string errorMsg = fetchResult.Error?.UserMessage ?? "خطا در دریافت وضعیت از بیمه‌گر.";
            logger.LogWarning("Refresh status failed for {PrescriptionId}: {Error}", prescription.Id, errorMsg);
            return RefreshProviderStatusResult.Failure(errorMsg);
        }

        PrescriptionStatus newCanonicalStatus = fetchResult.Data.CanonicalStatus;
        PrescriptionStatus previousStatus = prescription.Status;

        // Only update if the status transition is valid
        if (PrescriptionRecord.IsValidTransition(prescription.Status, newCanonicalStatus))
        {
            prescription.TransitionTo(newCanonicalStatus);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.WriteAsync(new AuditEntry
            {
                EntityType = nameof(PrescriptionRecord),
                EntityId = prescription.Id,
                Action = "StatusRefreshed",
                OldState = previousStatus.ToString(),
                NewState = newCanonicalStatus.ToString(),
                CorrelationId = request.CorrelationId,
                ProviderKey = prescription.SelectedProviderKey,
                OccurredAt = DateTime.UtcNow,
                TenantId = prescription.TenantId
            }, cancellationToken);

            logger.LogInformation("Prescription {PrescriptionId} status refreshed: {Old} → {New}",
                prescription.Id, previousStatus, newCanonicalStatus);
        }

        return RefreshProviderStatusResult.Success(newCanonicalStatus, fetchResult.Data.RemoteStatusText);
    }
}

public sealed class ReconcilePaperFallbackHandler(
    MedicalDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<ReconcilePaperFallbackHandler> logger)
    : IRequestHandler<ReconcilePaperFallbackCommand, ReconcilePaperFallbackResult>
{
    public async Task<ReconcilePaperFallbackResult> Handle(ReconcilePaperFallbackCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return ReconcilePaperFallbackResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status != PrescriptionStatus.PaperFallback)
        {
            return ReconcilePaperFallbackResult.Failure("نسخه در وضعیت 'کاغذی' نیست.");
        }

        PrescriptionStatus targetStatus = string.IsNullOrEmpty(request.RemotePrescriptionId)
            ? PrescriptionStatus.ManuallyReconciled
            : PrescriptionStatus.AcceptedByProvider;

        if (!PrescriptionRecord.IsValidTransition(PrescriptionStatus.PaperFallback, targetStatus))
        {
            return ReconcilePaperFallbackResult.Failure($"انتقال وضعیت به '{targetStatus}' مجاز نیست.");
        }

        prescription.TransitionTo(targetStatus);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = "PaperFallbackReconciled",
            OldState = PrescriptionStatus.PaperFallback.ToString(),
            NewState = targetStatus.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogInformation("Prescription {PrescriptionId} paper fallback reconciled → {Status}",
            prescription.Id, targetStatus);

        return ReconcilePaperFallbackResult.Success(targetStatus);
    }
}

public sealed class EscalateHandler(
    MedicalDbContext dbContext,
    IAuditWriter auditWriter,
    ILogger<EscalateHandler> logger)
    : IRequestHandler<EscalateCommand, EscalateResult>
{
    public async Task<EscalateResult> Handle(EscalateCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return EscalateResult.Failure("نسخه یافت نشد.");
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            return EscalateResult.Failure("خلاصه تشدید الزامی است.");
        }

        PrescriptionEscalation escalation = new()
        {
            Id = Guid.NewGuid(),
            PrescriptionId = request.PrescriptionId,
            OperatorId = request.OperatorId,
            Summary = request.Summary,
            StatusAtEscalation = prescription.Status,
            EscalatedAt = DateTime.UtcNow
        };

        _ = dbContext.Escalations.Add(escalation);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionEscalation),
            EntityId = escalation.Id,
            Action = "Escalated",
            OldState = prescription.Status.ToString(),
            CorrelationId = request.CorrelationId,
            ProviderKey = prescription.SelectedProviderKey,
            OccurredAt = DateTime.UtcNow,
            TenantId = prescription.TenantId
        }, cancellationToken);

        logger.LogInformation("Prescription {PrescriptionId} escalated by operator {OperatorId}",
            prescription.Id, request.OperatorId);

        return EscalateResult.Success(escalation.Id);
    }
}
