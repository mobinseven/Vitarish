namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ImportCodeSystemReleaseHandler(MedicalDbContext dbContext)
    : IRequestHandler<ImportCodeSystemReleaseCommand, EntityResult<ImportResult>>
{
    public async Task<EntityResult<ImportResult>> Handle(ImportCodeSystemReleaseCommand request, CancellationToken cancellationToken)
    {
        bool codeSystemExists = await dbContext.CodeSystems
            .AnyAsync(cs => cs.Id == request.CodeSystemId, cancellationToken);

        if (!codeSystemExists)
        {
            return EntityResult<ImportResult>.Failure(EntityError.Validation($"Code system '{request.CodeSystemId}' not found."));
        }

        if (!MedicalValidationRules.IsValidEffectiveDateRange(request.EffectiveFrom, request.EffectiveTo))
        {
            return EntityResult<ImportResult>.Failure(EntityError.Validation("EffectiveTo must be >= EffectiveFrom."));
        }

        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using IDbContextTransaction transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            // Create import batch
            var importBatch = new ImportBatch
            {
                Id = Guid.NewGuid(),
                SourceType = request.SourceType,
                SourceVersion = request.SourceVersion,
                SourceLocator = request.SourceLocator,
                SourceChecksum = request.SourceChecksum,
                Status = ImportBatchStatus.Validating,
                StartedAt = DateTimeOffset.UtcNow
            };

            // Check for duplicate import
            if (!string.IsNullOrEmpty(request.SourceChecksum))
            {
                bool checksumExists = await dbContext.ImportBatches
                    .AnyAsync(b => b.SourceChecksum == request.SourceChecksum, cancellationToken);

                if (checksumExists)
                {
                    return EntityResult<ImportResult>.Failure(EntityError.Validation("Import with this checksum already exists."));
                }
            }

            bool versionExists = await dbContext.ImportBatches
                .AnyAsync(b => b.SourceType == request.SourceType && b.SourceVersion == request.SourceVersion, cancellationToken);

            if (versionExists)
            {
                return EntityResult<ImportResult>.Failure(EntityError.Validation(
                    $"An import batch for source '{request.SourceType}' and version '{request.SourceVersion}' already exists."));
            }

            _ = await dbContext.ImportBatches.AddAsync(importBatch, cancellationToken);

            if (request.DryRun)
            {
                importBatch.Status = ImportBatchStatus.Pending;
                importBatch.ValidationSummary = $"Dry run: {request.Codes.Count} codes would be imported.";
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<ImportResult>.Success(
                    new ImportResult(
                        importBatch.Id,
                        importBatch.Status,
                        request.Codes.Count,
                        0,
                        request.Codes.Count,
                        Array.Empty<string>(),
                        Array.Empty<string>()));
            }

            // Create or find release
            CodeSystemRelease? release = await dbContext.CodeSystemReleases
                .FirstOrDefaultAsync(r => r.CodeSystemId == request.CodeSystemId && r.ReleaseName == request.ReleaseName, cancellationToken);

            if (release is null)
            {
                release = new CodeSystemRelease
                {
                    Id = Guid.NewGuid(),
                    CodeSystemId = request.CodeSystemId,
                    ReleaseName = request.ReleaseName,
                    ReleaseDate = request.ReleaseDate,
                    EffectiveFrom = request.EffectiveFrom,
                    EffectiveTo = request.EffectiveTo,
                    SourceLocator = request.SourceLocator,
                    SourceVersion = request.SourceVersion,
                    ImportBatchId = importBatch.Id,
                    IsActive = false
                };
                _ = await dbContext.CodeSystemReleases.AddAsync(release, cancellationToken);
            }

            // Import codes
            var codes = new List<MedicalCode>(request.Codes.Count);
            foreach (ImportMedicalCodeItem item in request.Codes)
            {
                if (!MedicalValidationRules.IsValidEffectiveDateRange(item.EffectiveFrom, item.EffectiveTo))
                {
                    importBatch.Status = ImportBatchStatus.Failed;
                    importBatch.ErrorReport = $"Invalid effective date range for code '{item.Code}'.";
                    _ = await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return EntityResult<ImportResult>.Failure(EntityError.Validation(importBatch.ErrorReport));
                }

                codes.Add(new MedicalCode
                {
                    Id = Guid.NewGuid(),
                    CodeSystemReleaseId = release.Id,
                    Code = item.Code,
                    DisplayName = item.DisplayName,
                    LongDescription = item.LongDescription,
                    Status = MedicalCodeStatus.Imported,
                    EffectiveFrom = item.EffectiveFrom,
                    EffectiveTo = item.EffectiveTo,
                    RecordOrigin = RecordOrigin.Imported,
                    SourceMetadataJson = item.SourceMetadataJson,
                    ImportBatchId = importBatch.Id
                });
            }

            await dbContext.MedicalCodes.AddRangeAsync(codes, cancellationToken);

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTimeOffset.UtcNow;
            importBatch.CompletedAt = DateTimeOffset.UtcNow;
            importBatch.ValidationSummary = $"Imported {codes.Count} codes.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<ImportResult>.Success(
                new ImportResult(
                    importBatch.Id,
                    importBatch.Status,
                    request.Codes.Count,
                    codes.Count,
                    Math.Max(request.Codes.Count - codes.Count, 0),
                    Array.Empty<string>(),
                    Array.Empty<string>()));
        });
    }
}
