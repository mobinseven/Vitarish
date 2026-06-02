namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ImportDrugCatalogHandler(MedicalDbContext dbContext)
    : IRequestHandler<ImportDrugCatalogCommand, EntityResult<ImportResult>>
{
    public async Task<EntityResult<ImportResult>> Handle(ImportDrugCatalogCommand request, CancellationToken cancellationToken)
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
                StartedAt = DateTimeOffset.UtcNow
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
                importBatch.ValidationSummary = $"Dry run: {request.Drugs.Count} drugs would be imported.";
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<ImportResult>.Success(
                    new ImportResult(
                        importBatch.Id,
                        importBatch.Status,
                        request.Drugs.Count,
                        0,
                        request.Drugs.Count,
                        Array.Empty<string>(),
                        Array.Empty<string>()));
            }

            int drugCount = 0;
            List<Drug> drugs = new(request.Drugs.Count);
            List<DrugProduct> products = [];
            List<PackagedDrug> packages = [];
            Dictionary<DrugImportKey, Guid> existingDrugs = await dbContext.Drugs.AsNoTracking()
                .Where(d => d.RecordOrigin == RecordOrigin.Imported)
                .ToDictionaryAsync(
                    d => new DrugImportKey(d.PrimaryIdentifierType, d.PrimaryIdentifierValue, d.EffectiveFrom),
                    d => d.Id,
                    cancellationToken);

            foreach (ImportDrugItem drugItem in request.Drugs)
            {
                if (!MedicalValidationRules.IsValidEffectiveDateRange(drugItem.EffectiveFrom, drugItem.EffectiveTo))
                {
                    importBatch.Status = ImportBatchStatus.Failed;
                    importBatch.ErrorReport = $"Invalid effective date range for drug '{drugItem.PrimaryName}'.";
                    _ = await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return EntityResult<ImportResult>.Failure(EntityError.Validation(importBatch.ErrorReport));
                }

                DrugImportKey drugKey = new(
                    drugItem.PrimaryIdentifierType,
                    drugItem.PrimaryIdentifierValue,
                    drugItem.EffectiveFrom);

                if (!existingDrugs.TryGetValue(drugKey, out Guid drugId))
                {
                    Drug drug = new()
                    {
                        Id = Guid.NewGuid(),
                        PrimaryName = drugItem.PrimaryName,
                        NormalizedName = drugItem.PrimaryName.ToUpperInvariant(),
                        PrimaryIdentifierType = drugItem.PrimaryIdentifierType,
                        PrimaryIdentifierValue = drugItem.PrimaryIdentifierValue,
                        Status = DrugStatus.Imported,
                        EffectiveFrom = drugItem.EffectiveFrom,
                        EffectiveTo = drugItem.EffectiveTo,
                        RecordOrigin = RecordOrigin.Imported,
                        ImportBatchId = importBatch.Id
                    };

                    drugs.Add(drug);
                    drugId = drug.Id;
                    existingDrugs[drugKey] = drugId;
                    drugCount++;
                }

                if (drugItem.Products is not null)
                {
                    foreach (ImportDrugProductItem productItem in drugItem.Products)
                    {
                        DrugProduct product = new()
                        {
                            Id = Guid.NewGuid(),
                            DrugId = drugId,
                            BrandName = productItem.BrandName,
                            GenericName = productItem.GenericName,
                            DoseForm = productItem.DoseForm,
                            Route = productItem.Route,
                            Strength = productItem.Strength,
                            Manufacturer = productItem.Manufacturer,
                            MarketStatus = productItem.MarketStatus,
                            RecordOrigin = RecordOrigin.Imported,
                            ImportBatchId = importBatch.Id
                        };

                        products.Add(product);

                        if (productItem.Packages is not null)
                        {
                            foreach (ImportPackagedDrugItem packageItem in productItem.Packages)
                            {
                                packages.Add(new PackagedDrug
                                {
                                    Id = Guid.NewGuid(),
                                    DrugProductId = product.Id,
                                    PackageIdentifier = packageItem.PackageIdentifier,
                                    IdentifierSource = packageItem.IdentifierSource,
                                    NdcCode = packageItem.NdcCode,
                                    PackageSize = packageItem.PackageSize,
                                    PackageUnit = packageItem.PackageUnit,
                                    PackageDescription = packageItem.PackageDescription,
                                    MarketingStartDate = packageItem.MarketingStartDate,
                                    MarketingEndDate = packageItem.MarketingEndDate,
                                    RecordOrigin = RecordOrigin.Imported,
                                    ImportBatchId = importBatch.Id
                                });
                            }
                        }
                    }
                }
            }

            await dbContext.Drugs.AddRangeAsync(drugs, cancellationToken);
            await dbContext.DrugProducts.AddRangeAsync(products, cancellationToken);
            await dbContext.PackagedDrugs.AddRangeAsync(packages, cancellationToken);

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTimeOffset.UtcNow;
            importBatch.CompletedAt = DateTimeOffset.UtcNow;
            importBatch.ValidationSummary = $"Imported {drugCount} drugs.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<ImportResult>.Success(
                new ImportResult(
                    importBatch.Id,
                    importBatch.Status,
                    request.Drugs.Count,
                    drugCount,
                    Math.Max(request.Drugs.Count - drugCount, 0),
                    Array.Empty<string>(),
                    Array.Empty<string>()));
        });
    }

    private readonly record struct DrugImportKey(
        DrugIdentifierType IdentifierType,
        string IdentifierValue,
        DateOnly? EffectiveFrom);
}
