namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ImportServiceValuationsHandler(MedicalDbContext dbContext)
    : IRequestHandler<ImportServiceValuationsCommand, EntityResult<ImportResult>>
{
    public async Task<EntityResult<ImportResult>> Handle(ImportServiceValuationsCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using IDbContextTransaction transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            ImportBatch importBatch = new()
            {
                Id = Guid.NewGuid(),
                SourceType = request.SourceType,
                SourceVersion = request.SourceVersion,
                SourceLocator = request.SourceLocator,
                SourceChecksum = request.SourceChecksum,
                Status = ImportBatchStatus.Validating,
                StartedAt = DateTime.UtcNow
            };

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
                importBatch.ValidationSummary = $"Dry run: {request.Valuations.Count} valuations would be imported.";
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<ImportResult>.Success(
                    new ImportResult(
                        importBatch.Id,
                        importBatch.Status,
                        request.Valuations.Count,
                        0,
                        request.Valuations.Count,
                        Array.Empty<string>(),
                        Array.Empty<string>()));
            }

            List<ServiceValuation> valuations = new(request.Valuations.Count);
            foreach (ImportServiceValuationItem item in request.Valuations)
            {
                if (!MedicalValidationRules.IsValidEffectiveDateRange(item.EffectiveFrom, item.EffectiveTo))
                {
                    importBatch.Status = ImportBatchStatus.Failed;
                    importBatch.ErrorReport = $"Invalid effective date range for service code '{item.ServiceCodeId}'.";
                    _ = await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return EntityResult<ImportResult>.Failure(EntityError.Validation(importBatch.ErrorReport ?? "Validation failed."));
                }

                valuations.Add(new ServiceValuation
                {
                    Id = Guid.NewGuid(),
                    ServiceCodeId = item.ServiceCodeId,
                    ValuationYear = item.ValuationYear,
                    Locality = item.Locality,
                    FacilityIndicator = item.FacilityIndicator,
                    WorkRvu = item.WorkRvu,
                    PracticeExpenseRvu = item.PracticeExpenseRvu,
                    MalpracticeRvu = item.MalpracticeRvu,
                    ConversionFactor = item.ConversionFactor,
                    PolicyIndicatorsJson = item.PolicyIndicatorsJson,
                    EffectiveFrom = item.EffectiveFrom,
                    EffectiveTo = item.EffectiveTo,
                    SourceReleaseId = request.SourceReleaseId,
                    RecordOrigin = RecordOrigin.Imported,
                    ImportBatchId = importBatch.Id
                });
            }

            await dbContext.ServiceValuations.AddRangeAsync(valuations, cancellationToken);

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTime.UtcNow;
            importBatch.CompletedAt = DateTime.UtcNow;
            importBatch.ValidationSummary = $"Imported {valuations.Count} valuations.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<ImportResult>.Success(
                new ImportResult(
                    importBatch.Id,
                    importBatch.Status,
                    request.Valuations.Count,
                    valuations.Count,
                    Math.Max(request.Valuations.Count - valuations.Count, 0),
                    Array.Empty<string>(),
                    Array.Empty<string>()));
        });
    }
}
