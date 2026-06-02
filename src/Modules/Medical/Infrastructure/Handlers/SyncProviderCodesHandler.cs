namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class SyncProviderCodesHandler(MedicalDbContext dbContext)
    : IRequestHandler<SyncProviderCodesCommand, EntityResult<ImportResult>>
{
    public async Task<EntityResult<ImportResult>> Handle(
        SyncProviderCodesCommand request,
        CancellationToken cancellationToken)
    {
        string systemKey = $"{request.ProviderKey.ToLowerInvariant()}.{request.CodeSystem.ToLowerInvariant()}";

        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using IDbContextTransaction transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            // Ensure the CodeSystem exists
            CodeSystem? codeSystem = await dbContext.CodeSystems
                .FirstOrDefaultAsync(cs => cs.SystemKey == systemKey, cancellationToken);

            if (codeSystem is null)
            {
                codeSystem = new CodeSystem
                {
                    Id = Guid.NewGuid(),
                    SystemKey = systemKey,
                    Name = $"{request.ProviderKey} {request.CodeSystem}",
                    Category = CodeSystemCategory.Other,
                    IsActive = true
                };
                _ = await dbContext.CodeSystems.AddAsync(codeSystem, cancellationToken);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Idempotency: skip if we already have a completed import for this version
            bool alreadyImported = await dbContext.ImportBatches
                .AnyAsync(
                    b => b.SourceType == ImportSourceType.InsuranceProvider
                        && b.SourceVersion == request.DataVersion
                        && b.Status == ImportBatchStatus.Imported,
                    cancellationToken);

            if (alreadyImported)
            {
                ImportBatch? existing = await dbContext.ImportBatches
                    .AsNoTracking()
                    .FirstAsync(
                        b => b.SourceType == ImportSourceType.InsuranceProvider
                            && b.SourceVersion == request.DataVersion,
                        cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return EntityResult<ImportResult>.Success(
                    new ImportResult(
                        existing.Id,
                        existing.Status,
                        request.Codes.Count,
                        0,
                        request.Codes.Count,
                        ["Version already imported — skipped."],
                        []));
            }

            // Create import batch
            var importBatch = new ImportBatch
            {
                Id = Guid.NewGuid(),
                SourceType = ImportSourceType.InsuranceProvider,
                SourceVersion = request.DataVersion,
                SourceLocator = systemKey,
                Status = ImportBatchStatus.Validating,
                StartedAt = DateTimeOffset.UtcNow
            };
            _ = await dbContext.ImportBatches.AddAsync(importBatch, cancellationToken);

            // Create the new release (inactive until codes are imported)
            var release = new CodeSystemRelease
            {
                Id = Guid.NewGuid(),
                CodeSystemId = codeSystem.Id,
                ReleaseName = request.DataVersion,
                ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SourceVersion = request.DataVersion,
                SourceLocator = systemKey,
                ImportBatchId = importBatch.Id,
                IsActive = false
            };
            _ = await dbContext.CodeSystemReleases.AddAsync(release, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            // Import codes
            var codes = new List<MedicalCode>(request.Codes.Count);
            foreach (SyncProviderCodeItem item in request.Codes)
            {
                codes.Add(new MedicalCode
                {
                    Id = Guid.NewGuid(),
                    CodeSystemReleaseId = release.Id,
                    Code = item.Code,
                    DisplayName = item.DisplayName,
                    Status = item.IsActive ? MedicalCodeStatus.Imported : MedicalCodeStatus.Retired,
                    RecordOrigin = RecordOrigin.Imported,
                    ImportBatchId = importBatch.Id
                });
            }

            await dbContext.MedicalCodes.AddRangeAsync(codes, cancellationToken);

            // Deactivate the previously active release(s) for this code system
            List<CodeSystemRelease> previousReleases = await dbContext.CodeSystemReleases
                .Where(r => r.CodeSystemId == codeSystem.Id && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (CodeSystemRelease prev in previousReleases)
            {
                prev.IsActive = false;
            }

            // Activate the new release
            release.IsActive = true;

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTimeOffset.UtcNow;
            importBatch.CompletedAt = DateTimeOffset.UtcNow;
            importBatch.ValidationSummary = $"Imported {codes.Count} codes from {systemKey}.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<ImportResult>.Success(
                new ImportResult(
                    importBatch.Id,
                    importBatch.Status,
                    request.Codes.Count,
                    codes.Count,
                    0,
                    [],
                    []));
        });
    }
}
