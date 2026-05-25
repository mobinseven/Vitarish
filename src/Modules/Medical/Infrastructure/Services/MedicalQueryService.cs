namespace Vitarish.Medical.Infrastructure.Services;

[RegisterTransient]
internal sealed class MedicalQueryService(MedicalDbContext dbContext) : IMedicalQueryService
{
    public async Task<CodeSystem?> GetCodeSystemAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.CodeSystems.AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);

    public async Task<CodeSystem?> GetCodeSystemByKeyAsync(string systemKey, CancellationToken cancellationToken)
        => await dbContext.CodeSystems.AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.SystemKey == systemKey, cancellationToken);

    public async Task<IReadOnlyList<CodeSystem>> ListCodeSystemsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        IQueryable<CodeSystem> query = dbContext.CodeSystems.AsNoTracking();
        if (activeOnly) query = query.Where(cs => cs.IsActive);
        return await query.OrderBy(cs => cs.Name).ToListAsync(cancellationToken);
    }

    public async Task<CodeSystemRelease?> GetCodeSystemReleaseAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.CodeSystemReleases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<CodeSystemRelease?> GetActiveReleaseAsync(string systemKey, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<CodeSystemRelease> query = dbContext.CodeSystemReleases.AsNoTracking()
            .Where(r => r.IsActive)
            .Where(r => dbContext.CodeSystems.Any(cs => cs.Id == r.CodeSystemId && cs.SystemKey == systemKey));

        if (asOfDate is { } date)
        {
            query = query.Where(r =>
                (r.EffectiveFrom == null || r.EffectiveFrom <= date) &&
                (r.EffectiveTo == null || r.EffectiveTo >= date));
        }

        return await query.OrderByDescending(r => r.ReleaseDate).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CodeSystemRelease>> ListReleasesAsync(Guid codeSystemId, bool activeOnly, CancellationToken cancellationToken)
    {
        IQueryable<CodeSystemRelease> query = dbContext.CodeSystemReleases.AsNoTracking()
            .Where(r => r.CodeSystemId == codeSystemId);

        if (activeOnly) query = query.Where(r => r.IsActive);
        return await query.OrderByDescending(r => r.ReleaseDate).ToListAsync(cancellationToken);
    }

    public async Task<MedicalCode?> ResolveMedicalCodeAsync(string systemKey, string code, Guid? releaseId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<MedicalCode> query = dbContext.MedicalCodes.AsNoTracking();

        if (releaseId.HasValue)
        {
            query = query.Where(c => c.CodeSystemReleaseId == releaseId.Value);
        }
        else
        {
            query = query.Where(c => dbContext.CodeSystemReleases.Any(r =>
                r.Id == c.CodeSystemReleaseId && r.IsActive &&
                dbContext.CodeSystems.Any(cs => cs.Id == r.CodeSystemId && cs.SystemKey == systemKey)));
        }

        query = query.Where(c => c.Code == code);

        if (asOfDate is { } date)
        {
            query = query.Where(c =>
                (c.EffectiveFrom == null || c.EffectiveFrom <= date) &&
                (c.EffectiveTo == null || c.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalCode>> SearchMedicalCodesAsync(string systemKey, string textOrCode, Guid? releaseId, bool activeOnly, DateOnly? asOfDate, int limit, CancellationToken cancellationToken)
    {
        IQueryable<MedicalCode> query = dbContext.MedicalCodes.AsNoTracking();

        if (releaseId.HasValue)
        {
            query = query.Where(c => c.CodeSystemReleaseId == releaseId.Value);
        }
        else
        {
            query = query.Where(c => dbContext.CodeSystemReleases.Any(r =>
                r.Id == c.CodeSystemReleaseId && r.IsActive &&
                dbContext.CodeSystems.Any(cs => cs.Id == r.CodeSystemId && cs.SystemKey == systemKey)));
        }

        query = query.Where(c => c.Code.Contains(textOrCode) || c.DisplayName.Contains(textOrCode));

        if (activeOnly)
        {
            query = query.Where(c => c.Status == MedicalCodeStatus.Active || c.Status == MedicalCodeStatus.Imported);
        }

        if (asOfDate is { } date)
        {
            query = query.Where(c =>
                (c.EffectiveFrom == null || c.EffectiveFrom <= date) &&
                (c.EffectiveTo == null || c.EffectiveTo >= date));
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<ImportBatch?> GetImportBatchAsync(Guid importBatchId, CancellationToken cancellationToken)
        => await dbContext.ImportBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == importBatchId, cancellationToken);

    public async Task<Drug?> GetDrugAsync(Guid drugId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<Drug> query = dbContext.Drugs.AsNoTracking().Where(d => d.Id == drugId);

        if (asOfDate is { } date)
        {
            query = query.Where(d =>
                (d.EffectiveFrom == null || d.EffectiveFrom <= date) &&
                (d.EffectiveTo == null || d.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Drug>> SearchDrugsAsync(string text, bool activeOnly, DateOnly? asOfDate, int limit, CancellationToken cancellationToken)
    {
        string normalizedText = text.ToUpperInvariant();
        IQueryable<Drug> query = dbContext.Drugs.AsNoTracking()
            .Where(d => d.NormalizedName.Contains(normalizedText) || d.PrimaryName.Contains(text));

        if (activeOnly)
        {
            query = query.Where(d => d.Status == DrugStatus.Active || d.Status == DrugStatus.Imported);
        }

        if (asOfDate is { } date)
        {
            query = query.Where(d =>
                (d.EffectiveFrom == null || d.EffectiveFrom <= date) &&
                (d.EffectiveTo == null || d.EffectiveTo >= date));
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<Drug?> ResolveDrugIdentifierAsync(DrugIdentifierType identifierType, string identifierValue, Guid? releaseId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<Drug> query = dbContext.Drugs.AsNoTracking()
            .Where(d => d.PrimaryIdentifierType == identifierType && d.PrimaryIdentifierValue == identifierValue);

        if (asOfDate is { } date)
        {
            query = query.Where(d =>
                (d.EffectiveFrom == null || d.EffectiveFrom <= date) &&
                (d.EffectiveTo == null || d.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DrugProduct?> GetDrugProductAsync(Guid drugProductId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<DrugProduct> query = dbContext.DrugProducts.AsNoTracking()
            .Where(p => p.Id == drugProductId);

        if (asOfDate is { } date)
        {
            query = query.Where(p =>
                (p.EffectiveFrom == null || p.EffectiveFrom <= date) &&
                (p.EffectiveTo == null || p.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PackagedDrug?> GetPackagedDrugAsync(Guid packagedDrugId, CancellationToken cancellationToken)
        => await dbContext.PackagedDrugs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == packagedDrugId, cancellationToken);

    public async Task<MedicalService?> GetMedicalServiceAsync(Guid serviceId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<MedicalService> query = dbContext.MedicalServices.AsNoTracking()
            .Where(s => s.Id == serviceId);

        if (asOfDate is { } date)
        {
            query = query.Where(s =>
                (s.EffectiveFrom == null || s.EffectiveFrom <= date) &&
                (s.EffectiveTo == null || s.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalService>> SearchMedicalServicesAsync(string text, string? category, string? specialty, bool activeOnly, DateOnly? asOfDate, int limit, CancellationToken cancellationToken)
    {
        IQueryable<MedicalService> query = dbContext.MedicalServices.AsNoTracking()
            .Where(s => s.Name.Contains(text) || (s.Description != null && s.Description.Contains(text)));

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(specialty))
        {
            query = query.Where(s => s.Specialty == specialty);
        }

        if (activeOnly)
        {
            query = query.Where(s => s.Status == MedicalServiceStatus.Active || s.Status == MedicalServiceStatus.Imported);
        }

        if (asOfDate is { } date)
        {
            query = query.Where(s =>
                (s.EffectiveFrom == null || s.EffectiveFrom <= date) &&
                (s.EffectiveTo == null || s.EffectiveTo >= date));
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<ServiceCode?> ResolveServiceCodeAsync(string systemKey, string code, Guid? releaseId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<ServiceCode> query = dbContext.ServiceCodes.AsNoTracking();

        if (releaseId.HasValue)
        {
            query = query.Where(sc => sc.CodeSystemReleaseId == releaseId.Value);
        }
        else
        {
            query = query.Where(sc => dbContext.CodeSystemReleases.Any(r =>
                r.Id == sc.CodeSystemReleaseId && r.IsActive &&
                dbContext.CodeSystems.Any(cs => cs.Id == r.CodeSystemId && cs.SystemKey == systemKey)));
        }

        query = query.Where(sc => sc.Code == code);

        if (asOfDate is { } date)
        {
            query = query.Where(sc =>
                (sc.EffectiveFrom == null || sc.EffectiveFrom <= date) &&
                (sc.EffectiveTo == null || sc.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceValuation?> GetServiceValuationAsync(Guid serviceCodeId, int year, string? locality, FacilityIndicator? facilityIndicator, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<ServiceValuation> query = dbContext.ServiceValuations.AsNoTracking()
            .Where(sv => sv.ServiceCodeId == serviceCodeId && sv.ValuationYear == year);

        if (!string.IsNullOrEmpty(locality))
        {
            query = query.Where(sv => sv.Locality == locality);
        }

        if (facilityIndicator.HasValue)
        {
            query = query.Where(sv => sv.FacilityIndicator == facilityIndicator.Value);
        }

        if (asOfDate is { } date)
        {
            query = query.Where(sv =>
                (sv.EffectiveFrom == null || sv.EffectiveFrom <= date) &&
                (sv.EffectiveTo == null || sv.EffectiveTo >= date));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CodeMap>> GetCodeMapsAsync(Guid sourceCodeId, string? targetSystemKey, MappingType? mappingType, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        IQueryable<CodeMap> query = dbContext.CodeMaps.AsNoTracking()
            .Where(m => m.SourceMedicalCodeId == sourceCodeId);

        if (!string.IsNullOrEmpty(targetSystemKey))
        {
            query = query.Where(m => dbContext.MedicalCodes.Any(c =>
                c.Id == m.TargetMedicalCodeId &&
                dbContext.CodeSystemReleases.Any(r =>
                    r.Id == c.CodeSystemReleaseId &&
                    dbContext.CodeSystems.Any(cs => cs.Id == r.CodeSystemId && cs.SystemKey == targetSystemKey))));
        }

        if (mappingType.HasValue)
        {
            query = query.Where(m => m.MappingType == mappingType.Value);
        }

        if (asOfDate is { } date)
        {
            query = query.Where(m =>
                (m.EffectiveFrom == null || m.EffectiveFrom <= date) &&
                (m.EffectiveTo == null || m.EffectiveTo >= date));
        }

        return await query.ToListAsync(cancellationToken);
    }
}
