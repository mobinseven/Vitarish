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
    IAuditWriter auditWriter,
    ILogger<CreateDraftHandler> logger)
    : IRequestHandler<CreateDraftCommand, CreateDraftResult>
{
    public async Task<CreateDraftResult> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
    {
        var prescription = new PrescriptionRecord
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            EncounterId = request.EncounterId,
            SelectedProviderKey = request.SelectedProviderKey,
            SelectedPatientCoverageId = request.SelectedPatientCoverageId,
            Status = PrescriptionStatus.Draft,
            VersionNumber = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantId = request.TenantId
        };

        dbContext.Prescriptions.Add(prescription);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            EntityType = nameof(PrescriptionRecord),
            EntityId = prescription.Id,
            Action = AuditAction.Created,
            NewState = PrescriptionStatus.Draft.ToString(),
            ProviderKey = request.SelectedProviderKey,
            OccurredAt = DateTimeOffset.UtcNow,
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
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return AddItemResult.Failure("نسخه یافت نشد.");

        if (prescription.Status != PrescriptionStatus.Draft)
            return AddItemResult.Failure("فقط نسخه‌های پیش‌نویس قابل ویرایش هستند.");

        var item = new PrescriptionItem
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
            CreatedAt = DateTimeOffset.UtcNow
        };

        prescription.Items.Add(item);
        prescription.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

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
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return RemoveItemResult.Failure("نسخه یافت نشد.");

        if (prescription.Status != PrescriptionStatus.Draft)
            return RemoveItemResult.Failure("فقط نسخه‌های پیش‌نویس قابل ویرایش هستند.");

        var item = prescription.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null)
            return RemoveItemResult.Failure("آیتم مورد نظر یافت نشد.");

        prescription.Items.Remove(item);
        prescription.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return RemoveItemResult.Success();
    }
}

public sealed class ValidateDraftHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<ValidateDraftCommand, ValidateDraftResult>
{
    public async Task<ValidateDraftResult> Handle(ValidateDraftCommand request, CancellationToken cancellationToken)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return new ValidateDraftResult { IsValid = false, Errors = ["نسخه یافت نشد."] };

        var errors = new List<string>();
        var warnings = new List<string>();

        // Payer-neutral validation
        if (prescription.Items.Count == 0)
            errors.Add("نسخه باید حداقل یک آیتم داشته باشد.");

        if (string.IsNullOrEmpty(prescription.SelectedProviderKey))
            errors.Add("بیمه‌گر مورد نظر انتخاب نشده است.");

        // Provider-specific validation
        if (!string.IsNullOrEmpty(prescription.SelectedProviderKey))
        {
            var adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
            if (adapter is null)
            {
                errors.Add("آداپتور بیمه‌گر انتخاب شده در دسترس نیست.");
            }
            else
            {
                var capabilities = adapter.GetCapabilities();

                // Check item categories against provider capabilities
                foreach (var item in prescription.Items)
                {
                    var requiredCapability = MapCategoryToCapability(item.Category);
                    if (requiredCapability.HasValue && !capabilities.Contains(requiredCapability.Value))
                    {
                        warnings.Add($"بیمه‌گر انتخابی از نوع آیتم «{item.Category}» پشتیبانی نمی‌کند.");
                    }
                }
            }
        }

        // If valid, transition to ReadyForSubmission
        if (errors.Count == 0 && prescription.Status == PrescriptionStatus.Draft)
        {
            prescription.TransitionTo(PrescriptionStatus.ReadyForSubmission);
            await dbContext.SaveChangesAsync(cancellationToken);
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
