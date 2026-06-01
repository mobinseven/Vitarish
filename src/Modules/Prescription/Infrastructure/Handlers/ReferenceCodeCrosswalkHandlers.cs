using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Application.Queries;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Services;

namespace Vitarish.Prescription.Infrastructure.Handlers;

/// <summary>Handler for upserting crosswalk mappings.</summary>
public sealed class UpsertReferenceCodeCrosswalkHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<UpsertReferenceCodeCrosswalkCommand, UpsertCrosswalkResult>
{
    public async Task<UpsertCrosswalkResult> Handle(
        UpsertReferenceCodeCrosswalkCommand request,
        CancellationToken cancellationToken)
    {
        // Validate provider code exists
        var providerCodeExists = await dbContext.ReferenceCodes
            .AnyAsync(rc => rc.ProviderKey == request.ProviderKey
                && rc.Code == request.ProviderCode
                && rc.IsActive, cancellationToken);

        if (!providerCodeExists)
            return UpsertCrosswalkResult.Failure($"Provider code '{request.ProviderCode}' not found or inactive for provider '{request.ProviderKey}'");

        // Check for existing mapping
        var existing = await dbContext.ReferenceCodeCrosswalks
            .FirstOrDefaultAsync(cw =>
                cw.LocalCode == request.LocalCode
                && cw.LocalCodeSystem == request.LocalCodeSystem
                && cw.ProviderKey == request.ProviderKey
                && cw.ProviderCode == request.ProviderCode
                && cw.ProviderCodeSystem == request.ProviderCodeSystem, cancellationToken);

        if (existing is not null)
        {
            // Update confidence and reactivate
            existing.MappingConfidence = request.MappingConfidence;
            existing.IsActive = true;
            existing.EffectiveTo = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return UpsertCrosswalkResult.Success(existing.Id);
        }

        // Create new crosswalk
        var crosswalk = new ReferenceCodeCrosswalk
        {
            Id = Guid.NewGuid(),
            LocalCode = request.LocalCode,
            LocalCodeSystem = request.LocalCodeSystem,
            ProviderKey = request.ProviderKey,
            ProviderCode = request.ProviderCode,
            ProviderCodeSystem = request.ProviderCodeSystem,
            MappingConfidence = request.MappingConfidence,
            IsActive = true,
            EffectiveFrom = DateTimeOffset.UtcNow
        };

        dbContext.Add(crosswalk);
        await dbContext.SaveChangesAsync(cancellationToken);

        return UpsertCrosswalkResult.Success(crosswalk.Id);
    }
}

/// <summary>Handler for deactivating a crosswalk.</summary>
public sealed class DeactivateReferenceCodeCrosswalkHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<DeactivateReferenceCodeCrosswalkCommand, bool>
{
    public async Task<bool> Handle(
        DeactivateReferenceCodeCrosswalkCommand request,
        CancellationToken cancellationToken)
    {
        var crosswalk = await dbContext.ReferenceCodeCrosswalks
            .FirstOrDefaultAsync(cw => cw.Id == request.CrosswalkId, cancellationToken);

        if (crosswalk is null)
            return false;

        crosswalk.IsActive = false;
        crosswalk.EffectiveTo = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Handler for bulk importing crosswalks from CSV.</summary>
public sealed class BulkImportCrosswalkHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<BulkImportCrosswalkCommand, BulkImportResult>
{
    public async Task<BulkImportResult> Handle(
        BulkImportCrosswalkCommand request,
        CancellationToken cancellationToken)
    {
        var lines = request.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int success = 0, skipped = 0, errorCount = 0;
        var errors = new List<string>();

        // Skip header line if it looks like a header
        var startIndex = lines.Length > 0 && lines[0].Contains("LocalCode", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        for (int i = startIndex; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 5)
            {
                errors.Add($"Line {i + 1}: Invalid format (expected at least 5 columns)");
                errorCount++;
                continue;
            }

            var localCode = parts[0].Trim();
            var localCodeSystem = parts[1].Trim();
            var providerKey = parts[2].Trim();
            var providerCode = parts[3].Trim();
            var providerCodeSystem = parts[4].Trim();
            var confidence = parts.Length > 5 && decimal.TryParse(parts[5].Trim(), out var c) ? c : 1.0m;

            // Check for duplicate
            var exists = await dbContext.ReferenceCodeCrosswalks
                .AnyAsync(cw =>
                    cw.LocalCode == localCode
                    && cw.LocalCodeSystem == localCodeSystem
                    && cw.ProviderKey == providerKey
                    && cw.ProviderCode == providerCode
                    && cw.ProviderCodeSystem == providerCodeSystem, cancellationToken);

            if (exists)
            {
                if (request.SkipDuplicates)
                {
                    skipped++;
                    continue;
                }
                errors.Add($"Line {i + 1}: Duplicate mapping for {localCode} → {providerCode}");
                errorCount++;
                continue;
            }

            dbContext.Add(new ReferenceCodeCrosswalk
            {
                Id = Guid.NewGuid(),
                LocalCode = localCode,
                LocalCodeSystem = localCodeSystem,
                ProviderKey = providerKey,
                ProviderCode = providerCode,
                ProviderCodeSystem = providerCodeSystem,
                MappingConfidence = confidence,
                IsActive = true,
                EffectiveFrom = DateTimeOffset.UtcNow
            });

            success++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new BulkImportResult
        {
            IsSuccess = errorCount == 0,
            SuccessCount = success,
            SkippedCount = skipped,
            ErrorCount = errorCount,
            Errors = errors
        };
    }
}

/// <summary>Handler for crosswalk queries by local code.</summary>
public sealed class GetReferenceCrosswalksByLocalCodeHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReferenceCrosswalksByLocalCodeQuery, IReadOnlyList<ProviderCodeMappingDto>>
{
    public async Task<IReadOnlyList<ProviderCodeMappingDto>> Handle(
        GetReferenceCrosswalksByLocalCodeQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReferenceCodeCrosswalks
            .AsNoTracking()
            .Where(cw => cw.LocalCode == request.LocalCode
                && cw.LocalCodeSystem == request.LocalCodeSystem);

        if (!string.IsNullOrEmpty(request.ProviderKey))
            query = query.Where(cw => cw.ProviderKey == request.ProviderKey);

        return await query
            .OrderByDescending(cw => cw.MappingConfidence)
            .Select(cw => new ProviderCodeMappingDto
            {
                CrosswalkId = cw.Id,
                ProviderKey = cw.ProviderKey,
                ProviderCode = cw.ProviderCode,
                Confidence = cw.MappingConfidence,
                IsActive = cw.IsActive,
                EffectiveFrom = cw.EffectiveFrom,
                EffectiveTo = cw.EffectiveTo
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Handler for code selector options (UI dropdown with provider filtering).</summary>
public sealed class GetCodeSelectorOptionsHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetCodeSelectorOptionsQuery, IReadOnlyList<CodeSelectorOptionDto>>
{
    public async Task<IReadOnlyList<CodeSelectorOptionDto>> Handle(
        GetCodeSelectorOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReferenceCodes
            .AsNoTracking()
            .Where(rc => rc.ProviderKey == request.ProviderKey
                && rc.CodeSystem == request.CodeSystem
                && rc.IsActive);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(rc =>
                rc.Code.Contains(request.SearchTerm) ||
                (rc.DisplayName != null && rc.DisplayName.Contains(request.SearchTerm)));

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(rc => rc.Category == request.Category);

        return await query
            .OrderBy(rc => rc.DisplayName ?? rc.Code)
            .Take(request.Take)
            .Select(rc => new CodeSelectorOptionDto
            {
                Code = rc.Code,
                DisplayName = rc.DisplayName ?? rc.Code,
                Category = rc.Category,
                IsAvailable = rc.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Handler for crosswalk admin grid.</summary>
public sealed class GetReferenceCodeCrosswalkGridHandler(PrescriptionDbContext dbContext)
    : IRequestHandler<GetReferenceCodeCrosswalkGridQuery, ReferenceCodeCrosswalkGridResult>
{
    public async Task<ReferenceCodeCrosswalkGridResult> Handle(
        GetReferenceCodeCrosswalkGridQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReferenceCodeCrosswalks.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.LocalCode))
            query = query.Where(cw => cw.LocalCode.Contains(request.LocalCode));

        if (!string.IsNullOrEmpty(request.ProviderKey))
            query = query.Where(cw => cw.ProviderKey == request.ProviderKey);

        if (request.ShowInactiveOnly == true)
            query = query.Where(cw => !cw.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(cw => cw.LocalCode)
            .ThenBy(cw => cw.ProviderKey)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(cw => new CrosswalkGridRowDto
            {
                Id = cw.Id,
                LocalCode = cw.LocalCode,
                LocalCodeSystem = cw.LocalCodeSystem,
                ProviderKey = cw.ProviderKey,
                ProviderCode = cw.ProviderCode,
                ProviderCodeSystem = cw.ProviderCodeSystem,
                Confidence = cw.MappingConfidence,
                IsActive = cw.IsActive,
                EffectiveFrom = cw.EffectiveFrom,
                EffectiveTo = cw.EffectiveTo
            })
            .ToListAsync(cancellationToken);

        return new ReferenceCodeCrosswalkGridResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
