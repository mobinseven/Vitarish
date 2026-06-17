using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Application.Abstractions.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class CreatePrescriptionTemplateHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<CreatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(CreatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        string? error = ValidateTenantAndName(request.TenantId, request.Name);
        if (error is not null) return PrescriptionTemplateCommandResult.Failure(error);

        PrescriptionTemplate template = new()
        {
            Id = Guid.NewGuid(), TenantId = request.TenantId.Trim(), Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description), CreatedAt = DateTime.UtcNow
        };
        _ = dbContext.PrescriptionTemplates.Add(template);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }

    internal static string? ValidateTenantAndName(string tenantId, string name)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return "شناسه مستأجر الزامی است.";
        if (string.IsNullOrWhiteSpace(name)) return "نام قالب نسخه الزامی است.";
        return name.Trim().Length > 256 ? "نام قالب نسخه نمی‌تواند بیش از ۲۵۶ نویسه باشد." : null;
    }

    internal static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdatePrescriptionTemplateHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<UpdatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(UpdatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        string? error = CreatePrescriptionTemplateHandler.ValidateTenantAndName(request.TenantId, request.Name);
        if (error is not null) return PrescriptionTemplateCommandResult.Failure(error);
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        if (!template.IsActive) return PrescriptionTemplateCommandResult.Failure("قالب بایگانی‌شده قابل ویرایش نیست.");
        template.Name = request.Name.Trim();
        template.Description = CreatePrescriptionTemplateHandler.NormalizeOptional(request.Description);
        template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class ArchivePrescriptionTemplateHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<ArchivePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(ArchivePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId)) return PrescriptionTemplateCommandResult.Failure("شناسه مستأجر الزامی است.");
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        template.Archive(DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class AddPrescriptionTemplateItemHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<AddPrescriptionTemplateItemCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(AddPrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 1) return PrescriptionTemplateCommandResult.Failure("تعداد آیتم باید حداقل یک باشد.");
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        if (!template.IsActive) return PrescriptionTemplateCommandResult.Failure("قالب بایگانی‌شده قابل ویرایش نیست.");
        PrescriptionTemplateItem item = new() { Id = Guid.NewGuid(), PrescriptionTemplateId = template.Id, Category = request.Category, ReferenceCode = request.ReferenceCode, DisplayName = request.DisplayName, DosageJson = request.DosageJson, Quantity = request.Quantity, DurationDays = request.DurationDays, MetadataJson = request.MetadataJson, SortOrder = NextSortOrder(template.Items), CreatedAt = DateTime.UtcNow };
        template.Items.Add(item); template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(item.Id);
    }

    internal static int NextSortOrder(IEnumerable<PrescriptionTemplateItem> items)
    {
        return CurrentMaxSortOrder(items) + 1;
    }

    private static int CurrentMaxSortOrder(IEnumerable<PrescriptionTemplateItem> items)
    {
        return items.Any() ? items.Max(i => i.SortOrder) : 0;
    }
}

public sealed class RemovePrescriptionTemplateItemHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<RemovePrescriptionTemplateItemCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(RemovePrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        PrescriptionTemplateItem? item = template.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null) return PrescriptionTemplateCommandResult.Failure("آیتم قالب یافت نشد.");
        _ = template.Items.Remove(item); template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class LoadPrescriptionTemplateIntoDraftHandler(PrescriptionDbContext dbContext, IInsuranceProviderRegistry providerRegistry)
    : IRequestHandler<LoadPrescriptionTemplateIntoDraftCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(LoadPrescriptionTemplateIntoDraftCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.AsNoTracking().Include(t => t.Items.OrderBy(i => i.SortOrder)).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null || !template.IsActive) return PrescriptionTemplateCommandResult.Failure("قالب فعال یافت نشد.");
        if (template.Items.Count == 0) return PrescriptionTemplateCommandResult.Failure("قالب نسخه آیتمی برای بارگذاری ندارد.");
        PrescriptionRecord? prescription = await dbContext.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == request.PrescriptionId && p.TenantId == request.TenantId, cancellationToken);
        if (prescription is null) return PrescriptionTemplateCommandResult.Failure("نسخه یافت نشد.");
        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection)) return PrescriptionTemplateCommandResult.Failure("قالب فقط در نسخه پیش‌نویس یا نیازمند اصلاح قابل بارگذاری است.");
        string? loadValidationError = ValidateLoad(template, prescription);
        if (loadValidationError is not null) return PrescriptionTemplateCommandResult.Failure(loadValidationError);
        int sortOrder = CurrentMaxSortOrder(prescription.Items);
        DateTime copiedAt = DateTime.UtcNow;
        foreach (PrescriptionTemplateItem templateItem in template.Items)
        {
            prescription.Items.Add(new PrescriptionItem { Id = Guid.NewGuid(), PrescriptionRecordId = prescription.Id, Category = templateItem.Category, ReferenceCode = templateItem.ReferenceCode, DisplayName = templateItem.DisplayName, DosageJson = templateItem.DosageJson, Quantity = templateItem.Quantity, DurationDays = templateItem.DurationDays, MetadataJson = templateItem.MetadataJson, SortOrder = ++sortOrder, CreatedAt = copiedAt });
        }
        prescription.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(prescription.Id);
    }
    private string? ValidateLoad(PrescriptionTemplate template, PrescriptionRecord prescription)
    {
        if (string.IsNullOrWhiteSpace(prescription.SelectedProviderKey)) return "بیمه‌گر مورد نظر انتخاب نشده است.";
        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null) return "آداپتور بیمه‌گر انتخاب شده در دسترس نیست.";
        IReadOnlySet<InsuranceProviderCapabilityType> capabilities = adapter.GetCapabilities();
        foreach (PrescriptionTemplateItem item in template.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ReferenceCode) && string.IsNullOrWhiteSpace(item.DisplayName)) return "آیتم قالب باید کد مرجع یا نام نمایشی داشته باشد.";
            if (item.Quantity < 1) return "تعداد آیتم باید حداقل یک باشد.";
            InsuranceProviderCapabilityType? requiredCapability = MapCategoryToCapability(item.Category);
            if (requiredCapability.HasValue && !capabilities.Contains(requiredCapability.Value)) return $"بیمه‌گر انتخابی از نوع آیتم «{item.Category}» پشتیبانی نمی‌کند.";
            if (prescription.Items.Any(existing => IsSamePrescriptionItem(existing, item))) return "بارگذاری قالب به دلیل آیتم تکراری انجام نشد.";
        }
        return null;
    }

    private static bool IsSamePrescriptionItem(PrescriptionItem existing, PrescriptionTemplateItem templateItem) =>
        existing.Category == templateItem.Category
        && string.Equals(existing.ReferenceCode?.Trim(), templateItem.ReferenceCode?.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(existing.DisplayName?.Trim(), templateItem.DisplayName?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static InsuranceProviderCapabilityType? MapCategoryToCapability(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Visit => InsuranceProviderCapabilityType.SupportsVisitPrescription,
        PrescriptionItemCategory.Drug => InsuranceProviderCapabilityType.SupportsDrugPrescription,
        PrescriptionItemCategory.Paraclinic or PrescriptionItemCategory.Lab or PrescriptionItemCategory.Imaging => InsuranceProviderCapabilityType.SupportsParaclinicPrescription,
        PrescriptionItemCategory.MedicalService => InsuranceProviderCapabilityType.SupportsMedicalServicePrescription,
        PrescriptionItemCategory.Physiotherapy => InsuranceProviderCapabilityType.SupportsPhysiotherapyPrescription,
        PrescriptionItemCategory.Dental => InsuranceProviderCapabilityType.SupportsDentalPrescription,
        PrescriptionItemCategory.Referral => InsuranceProviderCapabilityType.SupportsReferralPrescription,
        _ => null
    };

    private static int CurrentMaxSortOrder(IEnumerable<PrescriptionItem> items)
    {
        return items.Any() ? items.Max(i => i.SortOrder) : 0;
    }
}

public sealed class GetPrescriptionTemplatesHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetPrescriptionTemplatesQuery, IReadOnlyList<PrescriptionTemplateSummaryDto>>
{
    public async Task<IReadOnlyList<PrescriptionTemplateSummaryDto>> Handle(GetPrescriptionTemplatesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId)) return [];
        IQueryable<PrescriptionTemplate> query = dbContext.PrescriptionTemplates.AsNoTracking().Where(t => t.TenantId == request.TenantId);
        if (!request.IncludeArchived) query = query.Where(t => t.Status == PrescriptionTemplateStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm)) query = query.Where(t => t.Name.Contains(request.SearchTerm.Trim()));
        return await query.OrderBy(t => t.Name).Select(t => new PrescriptionTemplateSummaryDto { Id = t.Id, Name = t.Name, Description = t.Description, Status = t.Status, ItemCount = t.Items.Count, CreatedAt = t.CreatedAt }).ToListAsync(cancellationToken);
    }
}

public sealed class GetPrescriptionTemplateHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetPrescriptionTemplateQuery, PrescriptionTemplateDto?>
{
    public async Task<PrescriptionTemplateDto?> Handle(GetPrescriptionTemplateQuery request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.AsNoTracking().Include(t => t.Items.OrderBy(i => i.SortOrder)).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        return template is null ? null : new PrescriptionTemplateDto { Id = template.Id, Name = template.Name, Description = template.Description, Status = template.Status, ItemCount = template.Items.Count, CreatedAt = template.CreatedAt, Items = template.Items.Select(i => new PrescriptionTemplateItemDto { Id = i.Id, Category = i.Category, ReferenceCode = i.ReferenceCode, DisplayName = i.DisplayName, DosageJson = i.DosageJson, Quantity = i.Quantity, DurationDays = i.DurationDays, SortOrder = i.SortOrder }).ToList() };
    }
}
