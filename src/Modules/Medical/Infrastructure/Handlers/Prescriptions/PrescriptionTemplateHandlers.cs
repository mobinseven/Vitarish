using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Requests.Application.Abstractions;
using System.Text.Json;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class CreatePrescriptionTemplateHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<CreatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(CreatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireManagementCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

        string? error = ValidateTenantAndName(request.TenantId, request.Name);
        if (error is not null) return PrescriptionTemplateCommandResult.Failure(error);

        PrescriptionTemplate template = new()
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId.Trim(),
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            CreatedAt = DateTime.UtcNow
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

public sealed class UpdatePrescriptionTemplateHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<UpdatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(UpdatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireManagementCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

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

public sealed class ArchivePrescriptionTemplateHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<ArchivePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(ArchivePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireManagementCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

        if (string.IsNullOrWhiteSpace(request.TenantId)) return PrescriptionTemplateCommandResult.Failure("شناسه مستأجر الزامی است.");
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        template.Archive(DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class AddPrescriptionTemplateItemHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<AddPrescriptionTemplateItemCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(AddPrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireManagementCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

        if (string.IsNullOrWhiteSpace(request.ReferenceCode) && string.IsNullOrWhiteSpace(request.DisplayName)) return PrescriptionTemplateCommandResult.Failure("آیتم قالب باید کد مرجع یا نام نمایشی داشته باشد.");
        if (request.Quantity < 1) return PrescriptionTemplateCommandResult.Failure("تعداد آیتم باید حداقل یک باشد.");
        if (!HasValidClinicalMetadata(request.MetadataJson)) return PrescriptionTemplateCommandResult.Failure("فراداده بالینی آیتم قالب معتبر نیست.");
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        if (!template.IsActive) return PrescriptionTemplateCommandResult.Failure("قالب بایگانی‌شده قابل ویرایش نیست.");
        PrescriptionItemIdentity requestedIdentity = new(request.Category, request.ReferenceCode, request.DisplayName);
        if (template.Items.Any(item => PrescriptionItemIdentityComparer.AreSame(new PrescriptionItemIdentity(item.Category, item.ReferenceCode, item.DisplayName), requestedIdentity))) return PrescriptionTemplateCommandResult.Failure("آیتم تکراری در قالب نسخه مجاز نیست.");
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

    private static bool HasValidClinicalMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return true;
        try
        {
            using JsonDocument document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array || property.Value.GetRawText().Length > 256) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
}

public sealed class RemovePrescriptionTemplateItemHandler(PrescriptionDbContext dbContext, IRequestContext requestContext)
    : IRequestHandler<RemovePrescriptionTemplateItemCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(RemovePrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireManagementCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        PrescriptionTemplateItem? item = template.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null) return PrescriptionTemplateCommandResult.Failure("آیتم قالب یافت نشد.");
        _ = template.Items.Remove(item); template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class LoadPrescriptionTemplateIntoDraftHandler(PrescriptionDbContext dbContext, IInsuranceProviderRegistry providerRegistry, IMedicalQueryService medicalQueryService, IRequestContext requestContext)
    : IRequestHandler<LoadPrescriptionTemplateIntoDraftCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(LoadPrescriptionTemplateIntoDraftCommand request, CancellationToken cancellationToken)
    {
        string? authorizationError = PrescriptionTemplateAuthorization.RequireAuthoringCapability(requestContext, request.TenantId);
        if (authorizationError is not null) return PrescriptionTemplateCommandResult.Failure(authorizationError);

        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.AsNoTracking().Include(t => t.Items.OrderBy(i => i.SortOrder)).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null || !template.IsActive) return PrescriptionTemplateCommandResult.Failure("قالب فعال یافت نشد.");
        if (template.Items.Count == 0) return PrescriptionTemplateCommandResult.Failure("قالب نسخه آیتمی برای بارگذاری ندارد.");
        PrescriptionRecord? prescription = await dbContext.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == request.PrescriptionId && p.TenantId == request.TenantId, cancellationToken);
        if (prescription is null) return PrescriptionTemplateCommandResult.Failure("نسخه یافت نشد.");
        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection)) return PrescriptionTemplateCommandResult.Failure("قالب فقط در نسخه پیش‌نویس یا نیازمند اصلاح قابل بارگذاری است.");
        PrescriptionTemplateLoadValidationResult loadValidation = await ValidateLoadAsync(template, prescription, cancellationToken);
        if (!loadValidation.IsValid) return PrescriptionTemplateCommandResult.Failure("بارگذاری قالب به دلیل خطای اعتبارسنجی آیتم‌ها انجام نشد.", loadValidation.ItemFailures);
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
    private async Task<PrescriptionTemplateLoadValidationResult> ValidateLoadAsync(PrescriptionTemplate template, PrescriptionRecord prescription, CancellationToken cancellationToken)
    {
        List<PrescriptionTemplateItemFailure> failures = [];
        if (string.IsNullOrWhiteSpace(prescription.SelectedProviderKey)) return PrescriptionTemplateLoadValidationResult.Failure([TemplateFailure(template.Items.First(), "بیمه‌گر مورد نظر انتخاب نشده است.")]);
        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(prescription.SelectedProviderKey);
        if (adapter is null) return PrescriptionTemplateLoadValidationResult.Failure([TemplateFailure(template.Items.First(), "آداپتور بیمه‌گر انتخاب شده در دسترس نیست.")]);
        IReadOnlySet<InsuranceProviderCapabilityType> capabilities = adapter.GetCapabilities();
        List<PrescriptionItemIdentity> itemsToCheck = prescription.Items
            .Select(item => new PrescriptionItemIdentity(item.Category, item.ReferenceCode, item.DisplayName))
            .ToList();

        IReadOnlyDictionary<Guid, MedicalCodeResolution> resolvedCodes = await ResolveTemplateMedicalCodesAsync(template.Items, prescription.SelectedProviderKey, cancellationToken);

        foreach (PrescriptionTemplateItem item in template.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ReferenceCode) && string.IsNullOrWhiteSpace(item.DisplayName)) failures.Add(TemplateFailure(item, "آیتم قالب باید کد مرجع یا نام نمایشی داشته باشد."));
            if (item.Quantity < 1) failures.Add(TemplateFailure(item, "تعداد آیتم باید حداقل یک باشد."));
            InsuranceProviderCapabilityType? requiredCapability = MapCategoryToCapability(item.Category);
            if (!requiredCapability.HasValue) failures.Add(TemplateFailure(item, $"دسته آیتم «{item.Category}» قابل بارگذاری از قالب نیست."));
            if (requiredCapability.HasValue && !capabilities.Contains(requiredCapability.Value)) failures.Add(TemplateFailure(item, $"بیمه‌گر انتخابی از نوع آیتم «{item.Category}» پشتیبانی نمی‌کند."));
            PrescriptionItemIdentity templateIdentity = new(item.Category, item.ReferenceCode, item.DisplayName);
            if (itemsToCheck.Any(existing => PrescriptionItemIdentityComparer.AreSame(existing, templateIdentity))) failures.Add(TemplateFailure(item, "بارگذاری قالب به دلیل آیتم تکراری انجام نشد."));
            if (resolvedCodes.TryGetValue(item.Id, out MedicalCodeResolution? resolvedCode))
            {
                if (resolvedCode.MissingCodeSystem) failures.Add(TemplateFailure(item, $"دسته آیتم «{item.Category}» سامانه کد مرجع ندارد."));
                else if (resolvedCode.Code is null) failures.Add(TemplateFailure(item, "کد مرجع آیتم در منبع مرجع پزشکی فعال یافت نشد."));
                else if (resolvedCode.Code.Status is not (MedicalCodeStatus.Active or MedicalCodeStatus.Imported)) failures.Add(TemplateFailure(item, "کد مرجع آیتم در منبع مرجع پزشکی قابل استفاده نیست."));
            }
            itemsToCheck.Add(templateIdentity);
        }
        return failures.Count == 0 ? PrescriptionTemplateLoadValidationResult.Success() : PrescriptionTemplateLoadValidationResult.Failure(failures);
    }

    private async Task<IReadOnlyDictionary<Guid, MedicalCodeResolution>> ResolveTemplateMedicalCodesAsync(IEnumerable<PrescriptionTemplateItem> items, string providerKey, CancellationToken cancellationToken)
    {
        Task<MedicalCodeResolution>[] resolutionTasks = items
            .Where(item => !string.IsNullOrWhiteSpace(item.ReferenceCode))
            .Select(item => ResolveTemplateMedicalCodeAsync(item, providerKey, cancellationToken))
            .ToArray();

        MedicalCodeResolution[] resolutions = await Task.WhenAll(resolutionTasks);
        return resolutions.ToDictionary(resolution => resolution.TemplateItemId);
    }

    private async Task<MedicalCodeResolution> ResolveTemplateMedicalCodeAsync(PrescriptionTemplateItem item, string providerKey, CancellationToken cancellationToken)
    {
        string? codeSystemKey = MapCategoryToCodeSystemKey(providerKey, item.Category);
        if (codeSystemKey is null) return new MedicalCodeResolution(item.Id, null, true);

        MedicalCode? code = await medicalQueryService.ResolveMedicalCodeAsync(codeSystemKey, item.ReferenceCode!.Trim(), null, null, cancellationToken);
        return new MedicalCodeResolution(item.Id, code, false);
    }

    private static PrescriptionTemplateItemFailure TemplateFailure(PrescriptionTemplateItem item, string message) => new() { TemplateItemId = item.Id, Category = item.Category, ReferenceCode = item.ReferenceCode, DisplayName = item.DisplayName, Message = message };

    private sealed record MedicalCodeResolution(Guid TemplateItemId, MedicalCode? Code, bool MissingCodeSystem);

    private static string? MapCategoryToCodeSystemKey(string providerKey, PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => $"{providerKey.ToLowerInvariant()}.drug",
        PrescriptionItemCategory.Paraclinic or PrescriptionItemCategory.Lab or PrescriptionItemCategory.Imaging or PrescriptionItemCategory.MedicalService or PrescriptionItemCategory.Physiotherapy or PrescriptionItemCategory.Dental or PrescriptionItemCategory.Referral => $"{providerKey.ToLowerInvariant()}.service",
        _ => null
    };

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



internal static class PrescriptionTemplateAuthorization
{
    private const string AccessDeniedMessage = "دسترسی لازم برای عملیات قالب نسخه وجود ندارد.";

    internal static string? RequireManagementCapability(IRequestContext requestContext, string tenantId)
    {
        if (requestContext.IsPlatformAdmin()) return null;
        if (!IsSameTenant(requestContext, tenantId)) return AccessDeniedMessage;
        return requestContext.IsTenantOwner() ? null : AccessDeniedMessage;
    }

    internal static string? RequireAuthoringCapability(IRequestContext requestContext, string tenantId)
    {
        if (requestContext.IsPlatformAdmin()) return null;
        if (!IsSameTenant(requestContext, tenantId)) return AccessDeniedMessage;
        return requestContext.IsTenantOwner() || requestContext.IsTenantUser() ? null : AccessDeniedMessage;
    }

    private static bool IsSameTenant(IRequestContext requestContext, string tenantId)
        => !string.IsNullOrWhiteSpace(tenantId)
           && string.Equals(requestContext.TenantId, tenantId.Trim(), StringComparison.Ordinal);
}

internal sealed record PrescriptionTemplateLoadValidationResult(bool IsValid, IReadOnlyList<PrescriptionTemplateItemFailure> ItemFailures)
{
    internal static PrescriptionTemplateLoadValidationResult Success() => new(true, []);
    internal static PrescriptionTemplateLoadValidationResult Failure(IReadOnlyList<PrescriptionTemplateItemFailure> itemFailures) => new(false, itemFailures);
}

internal sealed record PrescriptionItemIdentity(PrescriptionItemCategory Category, string? ReferenceCode, string? DisplayName);

internal static class PrescriptionItemIdentityComparer
{
    internal static bool AreSame(PrescriptionItemIdentity existing, PrescriptionItemIdentity candidate)
    {
        if (existing.Category != candidate.Category) return false;

        bool hasExistingCode = !string.IsNullOrWhiteSpace(existing.ReferenceCode);
        bool hasCandidateCode = !string.IsNullOrWhiteSpace(candidate.ReferenceCode);

        if (hasExistingCode && hasCandidateCode)
        {
            return string.Equals(existing.ReferenceCode!.Trim(), candidate.ReferenceCode!.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        string? existingName = existing.DisplayName?.Trim();
        string? candidateName = candidate.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(existingName) || string.IsNullOrWhiteSpace(candidateName))
        {
            return false;
        }

        return string.Equals(existingName, candidateName, StringComparison.OrdinalIgnoreCase);
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
        return template is null ? null : new PrescriptionTemplateDto { Id = template.Id, Name = template.Name, Description = template.Description, Status = template.Status, ItemCount = template.Items.Count, CreatedAt = template.CreatedAt, Items = template.Items.Select(i => new PrescriptionTemplateItemDto { Id = i.Id, Category = i.Category, ReferenceCode = i.ReferenceCode, DisplayName = i.DisplayName, DosageJson = i.DosageJson, Quantity = i.Quantity, DurationDays = i.DurationDays, MetadataJson = i.MetadataJson, SortOrder = i.SortOrder }).ToList() };
    }
}
