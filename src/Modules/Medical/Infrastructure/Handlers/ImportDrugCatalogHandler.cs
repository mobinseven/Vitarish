namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ImportDrugCatalogHandler(MedicalDbContext dbContext)
    : IRequestHandler<ImportDrugCatalogCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(ImportDrugCatalogCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);

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

            if (!string.IsNullOrEmpty(request.SourceChecksum))
            {
                bool checksumExists = await dbContext.ImportBatches
                    .AnyAsync(b => b.SourceChecksum == request.SourceChecksum, cancellationToken);

                if (checksumExists)
                {
                    return EntityResult<Guid>.Failure(EntityError.Validation("Import with this checksum already exists."));
                }
            }

            _ = await dbContext.ImportBatches.AddAsync(importBatch, cancellationToken);

            if (request.DryRun)
            {
                importBatch.Status = ImportBatchStatus.Pending;
                importBatch.ValidationSummary = $"Dry run: {request.Drugs.Count} drugs would be imported.";
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<Guid>.Success(importBatch.Id);
            }

            int drugCount = 0;
            foreach (ImportDrugItem drugItem in request.Drugs)
            {
                var drug = new Drug
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

                _ = await dbContext.Drugs.AddAsync(drug, cancellationToken);
                drugCount++;

                if (drugItem.Products is not null)
                {
                    foreach (ImportDrugProductItem productItem in drugItem.Products)
                    {
                        var product = new DrugProduct
                        {
                            Id = Guid.NewGuid(),
                            DrugId = drug.Id,
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

                        _ = await dbContext.DrugProducts.AddAsync(product, cancellationToken);

                        if (productItem.Packages is not null)
                        {
                            foreach (ImportPackagedDrugItem packageItem in productItem.Packages)
                            {
                                var package = new PackagedDrug
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
                                };

                                _ = await dbContext.PackagedDrugs.AddAsync(package, cancellationToken);
                            }
                        }
                    }
                }
            }

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTimeOffset.UtcNow;
            importBatch.CompletedAt = DateTimeOffset.UtcNow;
            importBatch.ValidationSummary = $"Imported {drugCount} drugs.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<Guid>.Success(importBatch.Id);
        });
    }
}
