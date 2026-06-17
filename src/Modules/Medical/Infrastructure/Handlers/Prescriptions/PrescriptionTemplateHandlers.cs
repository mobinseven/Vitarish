using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

public sealed class CreatePrescriptionTemplateHandler(PrescriptionDbContext dbContext) : IRequestHandler<CreatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(CreatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId)) return PrescriptionTemplateCommandResult.Failure("شناسه مستأجر الزامی است.");
        string name;
        try { name = PrescriptionTemplate.NormalizeName(request.Name); } catch (ArgumentException ex) { return PrescriptionTemplateCommandResult.Failure(ex.Message); }
        DateTime now = DateTime.UtcNow;
        PrescriptionTemplate template = new() { Id = Guid.NewGuid(), TenantId = request.TenantId.Trim(), Name = name, Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(), Status = PrescriptionTemplateStatus.Active, CreatedAt = now };
        _ = dbContext.PrescriptionTemplates.Add(template);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class UpdatePrescriptionTemplateHandler(PrescriptionDbContext dbContext) : IRequestHandler<UpdatePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(UpdatePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        try { template.Rename(request.Name, request.Description, DateTime.UtcNow); } catch (ArgumentException ex) { return PrescriptionTemplateCommandResult.Failure(ex.Message); }
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class ArchivePrescriptionTemplateHandler(PrescriptionDbContext dbContext) : IRequestHandler<ArchivePrescriptionTemplateCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(ArchivePrescriptionTemplateCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        template.Archive(DateTime.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class AddPrescriptionTemplateItemHandler(PrescriptionDbContext dbContext) : IRequestHandler<AddPrescriptionTemplateItemCommand, PrescriptionTemplateItemCommandResult>
{
    public async Task<PrescriptionTemplateItemCommandResult> Handle(AddPrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateItemCommandResult.Failure("قالب نسخه یافت نشد.");
        if (!template.IsActive) return PrescriptionTemplateItemCommandResult.Failure("امکان ویرایش قالب آرشیوشده وجود ندارد.");
        if (request.Quantity <= 0) return PrescriptionTemplateItemCommandResult.Failure("تعداد آیتم باید بزرگتر از صفر باشد.");
        string displayName;
        try { displayName = PrescriptionTemplateItem.NormalizeDisplayName(request.DisplayName); } catch (ArgumentException ex) { return PrescriptionTemplateItemCommandResult.Failure(ex.Message); }
        PrescriptionTemplateItem item = new() { Id = Guid.NewGuid(), PrescriptionTemplateId = template.Id, Category = request.Category, ReferenceCode = string.IsNullOrWhiteSpace(request.ReferenceCode) ? null : request.ReferenceCode.Trim(), DisplayName = displayName, DosageJson = request.DosageJson, Quantity = request.Quantity, DurationDays = request.DurationDays, MetadataJson = request.MetadataJson, SortOrder = template.Items.Count + 1, CreatedAt = DateTime.UtcNow };
        template.Items.Add(item);
        template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateItemCommandResult.Success(item.Id);
    }
}

public sealed class RemovePrescriptionTemplateItemHandler(PrescriptionDbContext dbContext) : IRequestHandler<RemovePrescriptionTemplateItemCommand, PrescriptionTemplateCommandResult>
{
    public async Task<PrescriptionTemplateCommandResult> Handle(RemovePrescriptionTemplateItemCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null) return PrescriptionTemplateCommandResult.Failure("قالب نسخه یافت نشد.");
        PrescriptionTemplateItem? item = template.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item is null) return PrescriptionTemplateCommandResult.Failure("آیتم قالب یافت نشد.");
        _ = template.Items.Remove(item);
        template.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PrescriptionTemplateCommandResult.Success(template.Id);
    }
}

public sealed class LoadPrescriptionTemplateIntoDraftHandler(PrescriptionDbContext dbContext) : IRequestHandler<LoadPrescriptionTemplateIntoDraftCommand, LoadPrescriptionTemplateIntoDraftResult>
{
    public async Task<LoadPrescriptionTemplateIntoDraftResult> Handle(LoadPrescriptionTemplateIntoDraftCommand request, CancellationToken cancellationToken)
    {
        PrescriptionTemplate? template = await dbContext.PrescriptionTemplates.AsNoTracking().Include(t => t.Items.OrderBy(i => i.SortOrder)).FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TenantId == request.TenantId, cancellationToken);
        if (template is null || !template.IsActive) return LoadPrescriptionTemplateIntoDraftResult.Failure("قالب نسخه فعال یافت نشد.");
        if (template.Items.Count == 0) return LoadPrescriptionTemplateIntoDraftResult.Failure("قالب نسخه آیتمی برای بارگذاری ندارد.");
        PrescriptionRecord? prescription = await dbContext.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == request.PrescriptionId && p.TenantId == request.TenantId, cancellationToken);
        if (prescription is null) return LoadPrescriptionTemplateIntoDraftResult.Failure("نسخه یافت نشد.");
        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection)) return LoadPrescriptionTemplateIntoDraftResult.Failure("قالب فقط در نسخه پیش‌نویس قابل بارگذاری است.");
        string[] duplicates = template.Items.Where(ti => prescription.Items.Any(pi => pi.Category == ti.Category && pi.ReferenceCode == ti.ReferenceCode && pi.DisplayName == ti.DisplayName)).Select(ti => ti.DisplayName).ToArray();
        if (duplicates.Length > 0) return LoadPrescriptionTemplateIntoDraftResult.Failure($"آیتم تکراری در نسخه وجود دارد: {string.Join(", ", duplicates)}");
        int sortOrder = prescription.Items.Count;
        DateTime now = DateTime.UtcNow;
        foreach (PrescriptionTemplateItem templateItem in template.Items)
        {
            prescription.Items.Add(new PrescriptionItem { Id = Guid.NewGuid(), PrescriptionRecordId = prescription.Id, Category = templateItem.Category, ReferenceCode = templateItem.ReferenceCode, DisplayName = templateItem.DisplayName, DosageJson = templateItem.DosageJson, Quantity = templateItem.Quantity, DurationDays = templateItem.DurationDays, MetadataJson = templateItem.MetadataJson, SortOrder = ++sortOrder, CreatedAt = now });
        }
        prescription.UpdatedAt = now;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return LoadPrescriptionTemplateIntoDraftResult.Success(template.Items.Count);
    }
}

public sealed class GetPrescriptionTemplatesHandler(PrescriptionDbContext dbContext) : IRequestHandler<GetPrescriptionTemplatesQuery, IReadOnlyList<PrescriptionTemplateSummaryDto>>
{
    public async Task<IReadOnlyList<PrescriptionTemplateSummaryDto>> Handle(GetPrescriptionTemplatesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PrescriptionTemplate> query = dbContext.PrescriptionTemplates.AsNoTracking().Where(t => t.TenantId == request.TenantId);
        if (!request.IncludeArchived) query = query.Where(t => t.Status == PrescriptionTemplateStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.SearchText)) query = query.Where(t => t.Name.Contains(request.SearchText.Trim()));
        return await query.OrderBy(t => t.Name).Select(t => new PrescriptionTemplateSummaryDto { Id = t.Id, Name = t.Name, Description = t.Description, Status = t.Status, ItemCount = t.Items.Count, CreatedAt = t.CreatedAt }).ToListAsync(cancellationToken);
    }
}

public sealed class GetPrescriptionTemplateHandler(PrescriptionDbContext dbContext) : IRequestHandler<GetPrescriptionTemplateQuery, PrescriptionTemplateDto?>
{
    public async Task<PrescriptionTemplateDto?> Handle(GetPrescriptionTemplateQuery request, CancellationToken cancellationToken) => await dbContext.PrescriptionTemplates.AsNoTracking().Where(t => t.Id == request.TemplateId && t.TenantId == request.TenantId).Select(t => new PrescriptionTemplateDto { Id = t.Id, Name = t.Name, Description = t.Description, Status = t.Status, ItemCount = t.Items.Count, CreatedAt = t.CreatedAt, Items = t.Items.OrderBy(i => i.SortOrder).Select(i => new PrescriptionTemplateItemDto { Id = i.Id, Category = i.Category, ReferenceCode = i.ReferenceCode, DisplayName = i.DisplayName, DosageJson = i.DosageJson, Quantity = i.Quantity, DurationDays = i.DurationDays, SortOrder = i.SortOrder }).ToList() }).FirstOrDefaultAsync(cancellationToken);
}
