namespace Vitarish.Medical.Infrastructure.Services;

[RegisterTransient]
internal sealed class MedicalQueryService(MedicalDbContext dbContext) : IMedicalQueryService
{
    private const int MaxSearchLimit = 200;

    public async Task<CodeSystem?> GetCodeSystemAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.CodeSystems.AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);

    public async Task<CodeSystem?> GetCodeSystemByKeyAsync(string systemKey, CancellationToken cancellationToken)
        => await dbContext.CodeSystems.AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.SystemKey == systemKey, cancellationToken);

    public async Task<IReadOnlyList<CodeSystem>> ListCodeSystemsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        IQueryable<CodeSystem> query = dbContext.CodeSystems.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(cs => cs.IsActive);
        }

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

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query.OrderByDescending(r => r.ReleaseDate).ToListAsync(cancellationToken);
    }

    public async Task<MedicalCode?> ResolveMedicalCodeAsync(string systemKey, string code, Guid? releaseId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        if (!releaseId.HasValue)
        {
            CodeSystemRelease? activeRelease = await GetActiveReleaseAsync(systemKey, asOfDate, cancellationToken);
            if (activeRelease is null)
            {
                return null;
            }

            releaseId = activeRelease.Id;
        }

        IQueryable<MedicalCode> query = dbContext.MedicalCodes.AsNoTracking()
            .Where(c => c.CodeSystemReleaseId == releaseId.Value && c.Code == code);

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
        if (string.IsNullOrWhiteSpace(textOrCode))
        {
            return Array.Empty<MedicalCode>();
        }

        int boundedLimit = NormalizeSearchLimit(limit);

        if (!releaseId.HasValue)
        {
            CodeSystemRelease? activeRelease = await GetActiveReleaseAsync(systemKey, asOfDate, cancellationToken);
            if (activeRelease is null)
            {
                return Array.Empty<MedicalCode>();
            }

            releaseId = activeRelease.Id;
        }

        IQueryable<MedicalCode> query = dbContext.MedicalCodes.AsNoTracking()
            .Where(c => c.CodeSystemReleaseId == releaseId.Value && (c.Code.Contains(textOrCode) || c.DisplayName.Contains(textOrCode)));

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

        return await query
            .OrderBy(c => c.Code)
            .ThenBy(c => c.DisplayName)
            .ThenBy(c => c.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderReferenceSearchResult>> SearchPrescriptionReferenceCodesAsync(string providerKey, PrescriptionItemCategory? category, string textOrCode, bool activeOnly, DateOnly? asOfDate, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(textOrCode))
        {
            return [];
        }

        string normalizedProviderKey = providerKey.Trim().ToLowerInvariant();
        string normalizedText = Normalize(textOrCode);
        string[] systemKeys = category switch
        {
            PrescriptionItemCategory.Drug => [$"{normalizedProviderKey}.drug"],
            not null => [$"{normalizedProviderKey}.service"],
            null => [$"{normalizedProviderKey}.drug", $"{normalizedProviderKey}.service"]
        };
        int boundedLimit = NormalizeSearchLimit(limit);

        var rows = await (
            from code in dbContext.MedicalCodes.AsNoTracking()
            join release in dbContext.CodeSystemReleases.AsNoTracking() on code.CodeSystemReleaseId equals release.Id
            join system in dbContext.CodeSystems.AsNoTracking() on release.CodeSystemId equals system.Id
            where systemKeys.Contains(system.SystemKey) && release.IsActive
                  && (!activeOnly || code.Status == MedicalCodeStatus.Active || code.Status == MedicalCodeStatus.Imported)
                  && (code.NormalizedCode.Contains(normalizedText) || code.NormalizedDisplayName.Contains(normalizedText) || (code.NormalizedProviderCategory != null && code.NormalizedProviderCategory.Contains(normalizedText)))
                  && (asOfDate == null ||
                      (code.EffectiveFrom == null || code.EffectiveFrom <= asOfDate) &&
                      (code.EffectiveTo == null || code.EffectiveTo >= asOfDate))
            orderby code.NormalizedCode, code.NormalizedDisplayName, code.Id
            select new { system.SystemKey, code.Code, code.DisplayName, code.ProviderCategory, code.SourceMetadataJson })
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new ProviderReferenceSearchResult(
            providerKey.Trim(),
            row.SystemKey.EndsWith(".drug", StringComparison.Ordinal) ? "drug" : "service",
            row.SystemKey.EndsWith(".drug", StringComparison.Ordinal) ? PrescriptionItemCategory.Drug : PrescriptionItemCategory.MedicalService,
            row.Code,
            row.DisplayName,
            row.ProviderCategory,
            ReadSafeMetadata(row.SourceMetadataJson))).ToList();
    }

    public async Task<IReadOnlyList<ProviderReferenceSearchResult>> SearchProviderReferenceDataAsync(string providerKey, string codeSystem, string text, string? providerCategory, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(codeSystem) || string.IsNullOrWhiteSpace(text)) return [];

        string systemKey = $"{providerKey.Trim().ToLowerInvariant()}.{codeSystem.Trim().ToLowerInvariant()}";
        string normalizedText = Normalize(text);
        string? normalizedCategory = string.IsNullOrWhiteSpace(providerCategory) ? null : Normalize(providerCategory);
        int boundedLimit = NormalizeSearchLimit(limit);
        var rows = await (
            from code in dbContext.MedicalCodes.AsNoTracking()
            join release in dbContext.CodeSystemReleases.AsNoTracking() on code.CodeSystemReleaseId equals release.Id
            join system in dbContext.CodeSystems.AsNoTracking() on release.CodeSystemId equals system.Id
            where system.SystemKey == systemKey && release.IsActive
                  && (code.Status == MedicalCodeStatus.Active || code.Status == MedicalCodeStatus.Imported)
                  && (code.NormalizedCode.Contains(normalizedText) || code.NormalizedDisplayName.Contains(normalizedText) || (code.NormalizedProviderCategory != null && code.NormalizedProviderCategory.Contains(normalizedText)))
                  && (normalizedCategory == null || code.NormalizedProviderCategory == normalizedCategory)
            orderby code.NormalizedCode, code.NormalizedDisplayName, code.Id
            select new { code.Code, code.DisplayName, code.ProviderCategory, code.SourceMetadataJson })
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);

        PrescriptionItemCategory category = codeSystem.Trim().Equals("drug", StringComparison.OrdinalIgnoreCase)
            ? PrescriptionItemCategory.Drug : PrescriptionItemCategory.MedicalService;
        return rows.Select(row => new ProviderReferenceSearchResult(providerKey.Trim(), codeSystem.Trim(), category, row.Code, row.DisplayName, row.ProviderCategory, ReadSafeMetadata(row.SourceMetadataJson))).ToList();
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
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<Drug>();
        }

        int boundedLimit = NormalizeSearchLimit(limit);
        string normalizedText = text.ToUpperInvariant();
        IQueryable<Drug> query = dbContext.Drugs.AsNoTracking()
            .Where(d => d.NormalizedName.Contains(normalizedText));

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

        return await query
            .OrderBy(d => d.NormalizedName)
            .ThenBy(d => d.PrimaryIdentifierValue)
            .ThenBy(d => d.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Drug?> ResolveDrugIdentifierAsync(DrugIdentifierType identifierType, string identifierValue, DateOnly? asOfDate, CancellationToken cancellationToken)
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
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<MedicalService>();
        }

        int boundedLimit = NormalizeSearchLimit(limit);
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

        return await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCode?> ResolveServiceCodeAsync(string systemKey, string code, Guid? releaseId, DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        if (!releaseId.HasValue)
        {
            CodeSystemRelease? activeRelease = await GetActiveReleaseAsync(systemKey, asOfDate, cancellationToken);
            if (activeRelease is null)
            {
                return null;
            }

            releaseId = activeRelease.Id;
        }

        IQueryable<ServiceCode> query = dbContext.ServiceCodes.AsNoTracking()
            .Where(sc => sc.CodeSystemReleaseId == releaseId.Value && sc.Code == code);

        if (asOfDate is { } date)
        {
            query = query.Where(sc =>
                (sc.EffectiveFrom == null || sc.EffectiveFrom <= date) &&
                (sc.EffectiveTo == null || sc.EffectiveTo >= date));
        }

        return await query
            .OrderByDescending(sv => sv.EffectiveFrom)
            .ThenByDescending(sv => sv.Id)
            .FirstOrDefaultAsync(cancellationToken);
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
            query =
                from map in query
                join targetCode in dbContext.MedicalCodes.AsNoTracking() on map.TargetMedicalCodeId equals targetCode.Id
                join release in dbContext.CodeSystemReleases.AsNoTracking() on targetCode.CodeSystemReleaseId equals release.Id
                join codeSystem in dbContext.CodeSystems.AsNoTracking() on release.CodeSystemId equals codeSystem.Id
                where codeSystem.SystemKey == targetSystemKey
                select map;
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

    private static int NormalizeSearchLimit(int requestedLimit) => Math.Clamp(requestedLimit, 1, MaxSearchLimit);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static IReadOnlyDictionary<string, string?> ReadSafeMetadata(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new Dictionary<string, string?>();
        try { return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(value) ?? new Dictionary<string, string?>(); }
        catch (System.Text.Json.JsonException) { return new Dictionary<string, string?>(); }
    }
}
