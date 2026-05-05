using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalAuditWriter(
    ClinicalDbContext dbContext,
    ILogger<ClinicalAuditWriter> logger) : IClinicalAuditWriter
{
    public async Task WriteAsync(AuditWriteRequest request, CancellationToken cancellationToken = default)
    {
        string? detailsJson = null;

        if (request.Details is not null)
        {
            try
            {
                detailsJson = JsonSerializer.Serialize(request.Details);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not serialize audit entry details for action {Action}.", request.Action);
            }
        }

        dbContext.AuditEntries.Add(new ClinicalAuditEntry
        {
            Category = request.Category,
            Action = request.Action,
            ActorId = request.ActorId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            CorrelationId = request.CorrelationId,
            Reason = request.Reason,
            Outcome = request.Outcome,
            DetailsJson = detailsJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
