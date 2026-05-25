namespace Vitarish.Medical.Infrastructure.Handlers;

internal sealed class ImportServiceCatalogHandler(MedicalDbContext dbContext)
    : IRequestHandler<ImportServiceCatalogCommand, EntityResult<ImportResult>>
{
    public async Task<EntityResult<ImportResult>> Handle(ImportServiceCatalogCommand request, CancellationToken cancellationToken)
    {
        bool codeSystemExists = await dbContext.CodeSystems
            .AnyAsync(cs => cs.Id == request.CodeSystemId, cancellationToken);

        if (!codeSystemExists)
        {
            return EntityResult<ImportResult>.Failure(EntityError.Validation($"Code system '{request.CodeSystemId}' not found."));
        }

        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

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
                importBatch.ValidationSummary = $"Dry run: {request.Services.Count} services would be imported.";
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<ImportResult>.Success(
                    new ImportResult(
                        importBatch.Id,
                        importBatch.Status,
                        request.Services.Count,
                        0,
                        request.Services.Count,
                        Array.Empty<string>(),
                        Array.Empty<string>()));
            }

            // Find or create release
            CodeSystemRelease? release = await dbContext.CodeSystemReleases
                .FirstOrDefaultAsync(r => r.CodeSystemId == request.CodeSystemId && r.ReleaseName == request.ReleaseName, cancellationToken);

            if (release is null)
            {
                release = new CodeSystemRelease
                {
                    Id = Guid.NewGuid(),
                    CodeSystemId = request.CodeSystemId,
                    ReleaseName = request.ReleaseName,
                    SourceLocator = request.SourceLocator,
                    SourceVersion = request.SourceVersion,
                    ImportBatchId = importBatch.Id,
                    IsActive = false
                };
                _ = await dbContext.CodeSystemReleases.AddAsync(release, cancellationToken);
            }

            int serviceCount = 0;
            var services = new List<MedicalService>(request.Services.Count);
            var serviceCodes = new List<ServiceCode>();

            foreach (ImportServiceItem serviceItem in request.Services)
            {
                var service = new MedicalService
                {
                    Id = Guid.NewGuid(),
                    Name = serviceItem.Name,
                    Category = serviceItem.Category,
                    Specialty = serviceItem.Specialty,
                    Description = serviceItem.Description,
                    Status = MedicalServiceStatus.Imported,
                    RecordOrigin = RecordOrigin.Imported,
                    ImportBatchId = importBatch.Id
                };

                services.Add(service);
                serviceCount++;

                if (serviceItem.Codes is not null)
                {
                    foreach (ImportServiceCodeItem codeItem in serviceItem.Codes)
                    {
                        serviceCodes.Add(new ServiceCode
                        {
                            Id = Guid.NewGuid(),
                            MedicalServiceId = service.Id,
                            CodeSystemReleaseId = release.Id,
                            Code = codeItem.Code,
                            DisplayName = codeItem.DisplayName,
                            IsPrimary = codeItem.IsPrimary,
                            Status = ServiceCodeStatus.Imported,
                            RecordOrigin = RecordOrigin.Imported,
                            ImportBatchId = importBatch.Id
                        });
                    }
                }
            }

            await dbContext.MedicalServices.AddRangeAsync(services, cancellationToken);
            await dbContext.ServiceCodes.AddRangeAsync(serviceCodes, cancellationToken);

            importBatch.Status = ImportBatchStatus.Imported;
            importBatch.ImportedAt = DateTimeOffset.UtcNow;
            importBatch.CompletedAt = DateTimeOffset.UtcNow;
            importBatch.ValidationSummary = $"Imported {serviceCount} services.";

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return EntityResult<ImportResult>.Success(
                new ImportResult(
                    importBatch.Id,
                    importBatch.Status,
                    request.Services.Count,
                    serviceCount,
                    Math.Max(request.Services.Count - serviceCount, 0),
                    Array.Empty<string>(),
                    Array.Empty<string>()));
        });
    }
}
