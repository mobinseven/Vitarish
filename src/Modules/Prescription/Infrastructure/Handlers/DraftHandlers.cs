using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Handlers;

public sealed class CreateDraftHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IAuditWriter auditWriter,
    ILogger<CreateDraftHandler> logger)
    : IRequestHandler<CreateDraftCommand, CreateDraftResult>
{
    public async Task<CreateDraftResult> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
    {
        string? selectedProviderKey = string.IsNullOrWhiteSpace(request.SelectedProviderKey)
            ? null
            : request.SelectedProviderKey.Trim();

        if (selectedProviderKey is not null && !providerRegistry.IsProviderAvailable(selectedProviderKey))
        {
            return CreateDraftResult.Failure("بیمه‌گر انتخاب شده در سیستم موجود نیست.");
        }

        DateTime createdAt = DateTime.UtcNow;

        PrescriptionRecord prescription = new()
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            PatientNationalId = request.PatientNationalId,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            EncounterId = request.EncounterId,
            SelectedProviderKey = selectedProviderKey,
            Status = PrescriptionStatus.Draft,
            VersionNumber = 1,
            CreatedAt = createdAt,
            TenantId = request.TenantId
        };

        _ = dbContext.Prescriptions.Add(prescription);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = AuditAction.Created,
            NewState = PrescriptionStatus.Draft.ToString(),
            ProviderKey = selectedProviderKey,
            OccurredAt = createdAt,
            TenantId = request.TenantId
        }, cancellationToken);

        logger.LogInformation("Created draft prescription {PrescriptionId} for patient {PatientId}",
            prescription.Id, request.PatientId);

        return CreateDraftResult.Success(prescription.Id);
    }
}

public sealed class AddItemHandler(
    PrescriptionDbContext dbContext,
    ILogger<AddItemHandler> logger)
    : IRequestHandler<AddItemCommand, AddItemResult>
{
    public async Task<AddItemResult> Handle(AddItemCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return AddItemResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection))
        {
            return AddItemResult.Failure("فقط نسخه‌های پیش‌نویس یا نیازمند اصلاح قابل ویرایش هستند.");
        }

        PrescriptionItem item = new()
        {
            Id = Guid.NewGuid(),
            PrescriptionRecordId = prescription.Id,
            Category = request.Category,
            ReferenceCode = request.ReferenceCode,
            DisplayName = request.DisplayName,
            DosageJson = request.DosageJson,
            Quantity = request.Quantity,
            DurationDays = request.DurationDays,
            MetadataJson = request.MetadataJson,
            SortOrder = prescription.Items.Count + 1,
            CreatedAt = DateTime.UtcNow
        };

        prescription.Items.Add(item);
        prescription.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Added item {ItemId} to prescription {PrescriptionId}",
            item.Id, prescription.Id);

        return AddItemResult.Success(item.Id);
    }
}

public sealed class RemoveItemHandler(
    PrescriptionDbContext dbContext)
    : IRequestHandler<RemoveItemCommand, RemoveItemResult>
{
    public async Task<RemoveItemResult> Handle(RemoveItemCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return RemoveItemResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection))
        {
            return RemoveItemResult.Failure("فقط نسخه‌های پیش‌نویس یا نیازمند اصلاح قابل ویرایش هستند.");
        }

        PrescriptionItem? item = prescription.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null)
        {
            return RemoveItemResult.Failure("آیتم مورد نظر یافت نشد.");
        }

        _ = prescription.Items.Remove(item);
        prescription.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return RemoveItemResult.Success();
    }
}

public sealed class ValidateDraftHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<ValidateDraftCommand, ValidateDraftResult>
{
    private static readonly TimeSpan EligibilityFreshnessWindow = TimeSpan.FromHours(24);

    public async Task<ValidateDraftResult> Handle(ValidateDraftCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return new ValidateDraftResult { IsValid = false, Errors = ["نسخه یافت نشد."] };
        }

        List<string> errors = [];
        List<string> warnings = [];

        // Payer-neutral validation
        if (prescription.Items.Count == 0)
        {
            errors.Add("نسخه باید حداقل یک آیتم داشته باشد.");
        }

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            errors.Add("بیمه‌گر مورد نظر انتخاب نشده است.");
        }

        // Eligibility gate: must have confirmed eligible status that is not stale
        if (prescription.LastEligibilityStatus is null)
        {
            errors.Add("بررسی حق بیمه انجام نشده است. لطفاً ابتدا وضعیت بیمه بیمار را بررسی کنید.");
        }
        else if (prescription.LastEligibilityStatus != EligibilityStatus.Eligible)
        {
            errors.Add("بیمار واجد شرایط نیست. ارسال نسخه امکان‌پذیر نمی‌باشد.");
        }
        else if (prescription.LastEligibilityCheckedAt.HasValue &&
                 DateTime.UtcNow - prescription.LastEligibilityCheckedAt.Value > EligibilityFreshnessWindow)
        {
            warnings.Add("نتیجه بررسی حق بیمه قدیمی است. توصیه می‌شود مجدداً بررسی کنید.");
        }

        // Provider-specific validation
        if (!string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
            if (adapter is null)
            {
                errors.Add("آداپتور بیمه‌گر انتخاب شده در دسترس نیست.");
            }
            else
            {
                IReadOnlySet<InsuranceProviderCapabilityType> capabilities = adapter.GetCapabilities();

                // Check item categories against provider capabilities
                foreach (PrescriptionItem item in prescription.Items)
                {
                    InsuranceProviderCapabilityType? requiredCapability = MapCategoryToCapability(item.Category);
                    if (requiredCapability.HasValue && !capabilities.Contains(requiredCapability.Value))
                    {
                        warnings.Add($"بیمه‌گر انتخابی از نوع آیتم «{item.Category}» پشتیبانی نمی‌کند.");
                    }
                }
            }
        }

        // If valid, transition to ReadyForSubmission
        if (errors.Count == 0 && prescription.Status is PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection)
        {
            prescription.TransitionTo(PrescriptionStatus.ReadyForSubmission);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ValidateDraftResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static InsuranceProviderCapabilityType? MapCategoryToCapability(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Visit => InsuranceProviderCapabilityType.SupportsVisitPrescription,
        PrescriptionItemCategory.Drug => InsuranceProviderCapabilityType.SupportsDrugPrescription,
        PrescriptionItemCategory.Paraclinic or PrescriptionItemCategory.Lab or PrescriptionItemCategory.Imaging
            => InsuranceProviderCapabilityType.SupportsParaclinicPrescription,
        PrescriptionItemCategory.MedicalService => InsuranceProviderCapabilityType.SupportsMedicalServicePrescription,
        PrescriptionItemCategory.Physiotherapy => InsuranceProviderCapabilityType.SupportsPhysiotherapyPrescription,
        PrescriptionItemCategory.Dental => InsuranceProviderCapabilityType.SupportsDentalPrescription,
        PrescriptionItemCategory.Referral => InsuranceProviderCapabilityType.SupportsReferralPrescription,
        _ => null
    };
}

public sealed class RecordEligibilityHandler(
    PrescriptionDbContext dbContext)
    : IRequestHandler<RecordEligibilityCommand, RecordEligibilityResult>
{
    public async Task<RecordEligibilityResult> Handle(RecordEligibilityCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return RecordEligibilityResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection))
        {
            return RecordEligibilityResult.Failure("ثبت وضعیت بیمه فقط برای نسخه‌های پیش‌نویس امکان‌پذیر است.");
        }

        prescription.LastEligibilityStatus = request.Status;
        prescription.LastEligibilityCheckedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RecordEligibilityResult.Success();
    }
}

public sealed class SetProviderHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    ILogger<SetProviderHandler> logger)
    : IRequestHandler<SetProviderCommand, SetProviderResult>
{
    public async Task<SetProviderResult> Handle(SetProviderCommand request, CancellationToken cancellationToken)
    {
        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return SetProviderResult.Failure("نسخه یافت نشد.");
        }

        if (prescription.Status != PrescriptionStatus.Draft)
        {
            return SetProviderResult.Failure("انتخاب بیمه‌گر فقط برای نسخه‌های پیش‌نویس امکان‌پذیر است.");
        }

        if (string.IsNullOrWhiteSpace(request.ProviderKey))
        {
            return SetProviderResult.Failure("بیمه‌گر انتخاب شده در سیستم موجود نیست.");
        }

        string providerKey = request.ProviderKey.Trim();
        if (!providerRegistry.IsProviderAvailable(providerKey))
        {
            return SetProviderResult.Failure("بیمه‌گر انتخاب شده در سیستم موجود نیست.");
        }

        prescription.SelectedProviderKey = providerKey;
        prescription.UpdatedAt = DateTime.UtcNow;

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Provider set to {ProviderKey} for prescription {PrescriptionId}",
            providerKey, request.PrescriptionId);

        return SetProviderResult.Success();
    }
}
