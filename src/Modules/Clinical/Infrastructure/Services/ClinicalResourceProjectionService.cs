using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalResourceProjectionService(
    IFhirGateway gateway,
    ClinicalDbContext dbContext,
    ILogger<ClinicalResourceProjectionService> logger) : IClinicalResourceProjectionService
{
    public async Task<ProjectionReadDto> SyncAsync(
        string resourceType,
        string resourceId,
        Guid? resourceLinkId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

        string normalizedType = resourceType.Trim();
        string normalizedId = resourceId.Trim();

        ClinicalResourceProjection? existing = await dbContext.ResourceProjections
            .FirstOrDefaultAsync(p =>
                p.ResourceType == normalizedType &&
                p.ResourceId == normalizedId,
                cancellationToken);

        FhirOperationState state;
        string displayName = $"{normalizedType}/{normalizedId}";
        string? summary = null;
        string? patientReference = null;
        string? sourceVersionId = null;

        try
        {
            FhirOperationResponse response = await gateway.ExecuteAsync(new FhirOperationRequest
            {
                ResourceType = normalizedType,
                ResourceId = normalizedId,
                Interaction = FhirInteractionType.Read
            }, cancellationToken: cancellationToken);

            if (response.Status == FhirOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(response.ResponseJson))
            {
                ExtractDisplayFields(response.ResponseJson, normalizedType, ref displayName, ref summary, ref patientReference);
                sourceVersionId = response.RemoteVersionId;
            }

            state = response.Status == FhirOperationStatus.Succeeded
                ? FhirOperationState.Succeeded
                : FhirOperationState.Failed;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Projection sync failed for {ResourceType}/{ResourceId}.",
                normalizedType, normalizedId);
            state = FhirOperationState.Failed;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            existing.DisplayName = displayName;
            existing.Summary = summary;
            existing.PatientReference = patientReference;
            existing.SourceVersionId = sourceVersionId;
            existing.SyncState = state;
            existing.LastSyncedAt = now;
            existing.ExpiresAt = now.AddHours(24);
            if (resourceLinkId is not null) existing.ResourceLinkId = resourceLinkId;
        }
        else
        {
            existing = new ClinicalResourceProjection
            {
                ResourceType = normalizedType,
                ResourceId = normalizedId,
                ResourceLinkId = resourceLinkId,
                DisplayName = displayName,
                Summary = summary,
                PatientReference = patientReference,
                SourceVersionId = sourceVersionId,
                SyncState = state,
                LastSyncedAt = now,
                ExpiresAt = now.AddHours(24)
            };
            dbContext.ResourceProjections.Add(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<ProjectionReadDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ClinicalResourceProjection? projection = await dbContext.ResourceProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return projection is null ? null : Map(projection);
    }

    public async Task<IReadOnlyList<ProjectionReadDto>> ListAsync(ProjectionFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<ClinicalResourceProjection> query = dbContext.ResourceProjections.AsNoTracking();

        if (!filter.IncludeExpired)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            query = query.Where(p => p.ExpiresAt == null || p.ExpiresAt > now);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            string resourceType = filter.ResourceType.Trim();
            query = query.Where(p => p.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
        {
            string resourceId = filter.ResourceId.Trim();
            query = query.Where(p => p.ResourceId == resourceId);
        }

        if (filter.ResourceLinkId is not null)
        {
            query = query.Where(p => p.ResourceLinkId == filter.ResourceLinkId.Value);
        }

        if (filter.SyncState is not null)
        {
            query = query.Where(p => p.SyncState == filter.SyncState.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.PatientReference))
        {
            string patientRef = filter.PatientReference.Trim();
            query = query.Where(p => p.PatientReference == patientRef);
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        return await query
            .OrderByDescending(p => p.LastSyncedAt)
            .Take(take)
            .Select(p => new ProjectionReadDto
            {
                Id = p.Id,
                ResourceLinkId = p.ResourceLinkId,
                ResourceType = p.ResourceType,
                ResourceId = p.ResourceId,
                DisplayName = p.DisplayName,
                Summary = p.Summary,
                PatientReference = p.PatientReference,
                ClinicReference = p.ClinicReference,
                SourceVersionId = p.SourceVersionId,
                LastSyncedAt = p.LastSyncedAt,
                ExpiresAt = p.ExpiresAt,
                SyncState = p.SyncState
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ExpireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ClinicalResourceProjection? projection = await dbContext.ResourceProjections
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Projection '{id}' was not found.");

        projection.ExpiresAt = DateTimeOffset.UtcNow;
        projection.SyncState = FhirOperationState.Skipped;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ExtractDisplayFields(
        string responseJson,
        string resourceType,
        ref string displayName,
        ref string? summary,
        ref string? patientReference)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            JsonElement root = doc.RootElement;

            if (string.Equals(resourceType, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                string? familyName = null, givenName = null;
                if (root.TryGetProperty("name", out JsonElement names) && names.GetArrayLength() > 0)
                {
                    JsonElement firstName = names[0];
                    familyName = firstName.TryGetProperty("family", out JsonElement fam) ? fam.GetString() : null;
                    givenName = firstName.TryGetProperty("given", out JsonElement given) && given.GetArrayLength() > 0
                        ? given[0].GetString()
                        : null;
                }

                string composed = string.Join(" ", new[] { givenName, familyName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(composed)) displayName = composed;

                if (root.TryGetProperty("gender", out JsonElement gender)) summary = gender.GetString();
            }
            else if (root.TryGetProperty("subject", out JsonElement subject) &&
                     subject.TryGetProperty("reference", out JsonElement subjectRef))
            {
                patientReference = subjectRef.GetString();
            }

            if (root.TryGetProperty("text", out JsonElement text) &&
                text.TryGetProperty("div", out JsonElement div))
            {
                string? divText = div.GetString();
                if (!string.IsNullOrWhiteSpace(divText) && summary is null)
                {
                    summary = divText.Length > 200 ? string.Concat(divText.AsSpan(0, 200), "…") : divText;
                }
            }
        }
        catch
        {
            // Best-effort extraction; silent failure is intentional
        }
    }

    private static ProjectionReadDto Map(ClinicalResourceProjection p) => new()
    {
        Id = p.Id,
        ResourceLinkId = p.ResourceLinkId,
        ResourceType = p.ResourceType,
        ResourceId = p.ResourceId,
        DisplayName = p.DisplayName,
        Summary = p.Summary,
        PatientReference = p.PatientReference,
        ClinicReference = p.ClinicReference,
        SourceVersionId = p.SourceVersionId,
        LastSyncedAt = p.LastSyncedAt,
        ExpiresAt = p.ExpiresAt,
        SyncState = p.SyncState
    };
}
