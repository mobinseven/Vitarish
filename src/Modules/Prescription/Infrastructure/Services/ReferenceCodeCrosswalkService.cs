using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Services;

/// <summary>
/// Manages reference code crosswalks: resolution, validation, and provider code lookup.
/// </summary>
public sealed class ReferenceCodeCrosswalkService(
    PrescriptionDbContext dbContext,
    ILogger<ReferenceCodeCrosswalkService> logger)
{
    /// <summary>
    /// Resolves a local code to the best-matching provider code.
    /// Returns null if no active mapping exists with sufficient confidence.
    /// </summary>
    public async Task<ReferenceCode?> GetPreferredProviderCodeAsync(
        string localCode,
        string localCodeSystem,
        string providerKey,
        decimal minConfidence = 0.8m,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var crosswalk = await dbContext.ReferenceCodeCrosswalks
            .Where(cw => cw.LocalCode == localCode
                && cw.LocalCodeSystem == localCodeSystem
                && cw.ProviderKey == providerKey
                && cw.IsActive
                && cw.MappingConfidence >= minConfidence
                && (cw.EffectiveFrom == null || cw.EffectiveFrom <= now)
                && (cw.EffectiveTo == null || cw.EffectiveTo > now))
            .OrderByDescending(cw => cw.MappingConfidence)
            .FirstOrDefaultAsync(cancellationToken);

        if (crosswalk is null)
            return null;

        return await dbContext.ReferenceCodes
            .Where(rc => rc.ProviderKey == providerKey
                && rc.Code == crosswalk.ProviderCode
                && rc.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves a local code to all active provider codes (sorted by confidence descending).
    /// </summary>
    public async Task<IReadOnlyList<(ReferenceCodeCrosswalk Crosswalk, ReferenceCode? Code)>> ResolveProviderCodesAsync(
        string localCode,
        string localCodeSystem,
        string? providerKey,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var query = dbContext.ReferenceCodeCrosswalks
            .Where(cw => cw.LocalCode == localCode
                && cw.LocalCodeSystem == localCodeSystem
                && cw.IsActive
                && (cw.EffectiveFrom == null || cw.EffectiveFrom <= now)
                && (cw.EffectiveTo == null || cw.EffectiveTo > now));

        if (!string.IsNullOrEmpty(providerKey))
            query = query.Where(cw => cw.ProviderKey == providerKey);

        var crosswalks = await query
            .OrderByDescending(cw => cw.MappingConfidence)
            .ToListAsync(cancellationToken);

        var results = new List<(ReferenceCodeCrosswalk, ReferenceCode?)>();

        foreach (var cw in crosswalks)
        {
            var code = await dbContext.ReferenceCodes
                .Where(rc => rc.ProviderKey == cw.ProviderKey
                    && rc.Code == cw.ProviderCode
                    && rc.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            results.Add((cw, code));
        }

        return results;
    }

    /// <summary>
    /// Detects broken crosswalks: active crosswalks pointing to inactive or missing codes.
    /// </summary>
    public async Task<IReadOnlyList<(ReferenceCodeCrosswalk Crosswalk, string Reason)>> DetectBrokenCrosswalksAsync(
        string? providerKey, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ReferenceCodeCrosswalks.Where(cw => cw.IsActive);

        if (!string.IsNullOrEmpty(providerKey))
            query = query.Where(cw => cw.ProviderKey == providerKey);

        var crosswalks = await query.ToListAsync(cancellationToken);
        var broken = new List<(ReferenceCodeCrosswalk, string)>();

        foreach (var cw in crosswalks)
        {
            var code = await dbContext.ReferenceCodes
                .Where(rc => rc.ProviderKey == cw.ProviderKey && rc.Code == cw.ProviderCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (code is null)
            {
                broken.Add((cw, "Provider code not found"));
            }
            else if (!code.IsActive)
            {
                broken.Add((cw, "Provider code is inactive/deactivated"));
            }
        }

        logger.LogInformation("Detected {Count} broken crosswalks for provider {Provider}",
            broken.Count, providerKey ?? "all");

        return broken;
    }
}
