using Vitarish.Entities.Contracts.Models;

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

            // Idempotency: handle any existing import for this logical version before inserting a new batch.
            ImportBatch? existingBatch = await dbContext.ImportBatches
                .FirstOrDefaultAsync(
                    b => b.SourceType == ImportSourceType.InsuranceProvider
                        && b.SourceVersion == request.DataVersion
                        && b.SourceLocator == systemKey,
                    cancellationToken);

            if (existingBatch is not null)
            {
                if (existingBatch.Status == ImportBatchStatus.Imported)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return EntityResult<ImportResult>.Success(
                        new ImportResult(
                            existingBatch.Id,
                            existingBatch.Status,
                            request.Codes.Count,
                            0,
                            request.Codes.Count,
                            ["Version already imported — skipped."],
                            []));
                }

                List<CodeSystemRelease> incompleteReleases = await dbContext.CodeSystemReleases
                    .Where(r => r.ImportBatchId == existingBatch.Id)
                    .ToListAsync(cancellationToken);

                List<Guid> incompleteReleaseIds = [.. incompleteReleases.Select(r => r.Id)];
                List<MedicalCode> incompleteCodes = await dbContext.MedicalCodes
                    .Where(c => c.ImportBatchId == existingBatch.Id
                        || incompleteReleaseIds.Contains(c.CodeSystemReleaseId))
                    .ToListAsync(cancellationToken);

                foreach (MedicalCode code in incompleteCodes)
                {
                    code.HardDelete();
                }

                dbContext.MedicalCodes.RemoveRange(incompleteCodes);

                foreach (CodeSystemRelease incompleteRelease in incompleteReleases)
                {
                    incompleteRelease.HardDelete();
                }

                dbContext.CodeSystemReleases.RemoveRange(incompleteReleases);

                existingBatch.HardDelete();
                _ = dbContext.ImportBatches.Remove(existingBatch);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Create import batch
            ImportBatch importBatch = new()
            {
                Id = Guid.NewGuid(),
                SourceType = ImportSourceType.InsuranceProvider,
                SourceVersion = request.DataVersion,
                SourceLocator = systemKey,
                Status = ImportBatchStatus.Validating,
                StartedAt = DateTime.UtcNow
            };
            _ = await dbContext.ImportBatches.AddAsync(importBatch, cancellationToken);

            // Create the new release (inactive until codes are imported)
            CodeSystemRelease release = new()
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
            List<MedicalCode> codes = new(request.Codes.Count);
            foreach (SyncProviderCodeItem item in request.Codes)
            {
                codes.Add(new MedicalCode
                {
                    Id = Guid.NewGuid(),
                    CodeSystemReleaseId = release.Id,
                    Code = item.Code,
                    DisplayName = item.DisplayName,
                    ProviderCategory = item.Category,
                    NormalizedCode = Normalize(item.Code),
                    NormalizedDisplayName = Normalize(item.DisplayName),
                    NormalizedProviderCategory = NormalizeOptional(item.Category),
                    SourceMetadataJson = item.SafeMetadataJson,
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
            importBatch.ImportedAt = DateTime.UtcNow;
            importBatch.CompletedAt = DateTime.UtcNow;
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

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
