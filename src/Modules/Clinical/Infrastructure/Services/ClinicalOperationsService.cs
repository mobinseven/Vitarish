using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalOperationsService(
    ClinicalDbContext dbContext,
    IFhirGateway gateway,
    IRequestContext requestContext,
    IClinicalAuditWriter auditWriter,
    IOptionsMonitor<ClinicalOptions> options) : IClinicalOperationsService
{
    private readonly ClinicalOptions _options = options.CurrentValue;

    public async Task<FhirOperationResponse> ExecuteAsync(FhirOperationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceType);

        string normalizedResourceType = request.ResourceType.Trim();
        string correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? requestContext.RequestId ?? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();

        FhirInteractionType interaction = request.Interaction;
        FhirOperationRequest normalizedRequest = request with
        {
            ResourceType = normalizedResourceType,
            ResourceId = request.ResourceId?.Trim(),
            CorrelationId = correlationId,
            IdempotencyKey = request.IdempotencyKey?.Trim()
        };

        ClinicalFhirOperation? existingOperation = await FindExistingIdempotentOperationAsync(normalizedRequest, cancellationToken);
        if (existingOperation is not null)
        {
            existingOperation.RetryCount += 1;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(existingOperation);
        }

        ClinicalFhirOperation operation = new()
        {
            CorrelationId = correlationId,
            IdempotencyKey = normalizedRequest.IdempotencyKey,
            Interaction = interaction,
            RequestJson = JsonSerializer.Serialize(normalizedRequest),
            ResourceId = normalizedRequest.ResourceId,
            ResourceType = normalizedResourceType,
            RetryCount = 0,
            StartedAt = DateTimeOffset.UtcNow,
            Status = FhirOperationStatus.Pending
        };

        dbContext.FhirOperations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!IsAllowedResourceType(normalizedResourceType) || !IsAllowedOperation(interaction))
        {
            operation.Status = FhirOperationStatus.Denied;
            operation.FailureKind = FhirFailureKind.Denied;
            operation.FailureMessage = BuildDeniedMessage(normalizedResourceType, interaction);
            operation.CompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(operation);
        }

        operation.Status = FhirOperationStatus.Running;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            FhirOperationResponse response = await gateway.ExecuteAsync(normalizedRequest, cancellationToken: cancellationToken);
            operation.Status = response.Status;
            operation.FailureKind = response.FailureKind;
            operation.FailureMessage = response.FailureMessage;
            operation.ResponseJson = response.ResponseJson;
            operation.OutcomeJson = response.OutcomeJson;
            operation.ResponseStatusCode = response.ResponseStatusCode;
            operation.ResultCount = response.ResultCount;
            operation.RemoteVersionId = response.RemoteVersionId;
            operation.ETag = response.ETag;
            operation.CompletedAt = response.CompletedAt ?? DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(normalizedRequest, operation, outcome: response.Status.ToString(), cancellationToken);
            return response with { OperationId = operation.Id, StartedAt = operation.StartedAt };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            operation.Status = FhirOperationStatus.Failed;
            operation.FailureKind = ClassifyFailure(exception);
            operation.FailureMessage = exception.Message;
            operation.CompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(normalizedRequest, operation, outcome: "Failed", cancellationToken);
            return Map(operation);
        }
    }

    private Task WriteAuditAsync(FhirOperationRequest request, ClinicalFhirOperation operation, string outcome, CancellationToken cancellationToken)
    {
        return auditWriter.WriteAsync(new AuditWriteRequest
        {
            Category = "FhirOperation",
            Action = request.Interaction.ToString(),
            ActorId = requestContext.UserId,
            CorrelationId = request.CorrelationId,
            Outcome = outcome,
            Reason = request.AccessReason,
            ResourceId = request.ResourceId,
            ResourceType = request.ResourceType,
            Details = new { operation.Id, request.IdempotencyKey, operation.FailureKind }
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalFhirOperationRecord>> ListAsync(ClinicalFhirOperationFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<ClinicalFhirOperation> query = dbContext.FhirOperations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            string resourceType = filter.ResourceType.Trim();
            query = query.Where(operation => operation.ResourceType == resourceType);
        }

        if (filter.Interaction is not null)
        {
            query = query.Where(operation => operation.Interaction == filter.Interaction.Value);
        }

        if (filter.Status is not null)
        {
            query = query.Where(operation => operation.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string searchText = filter.SearchText.Trim();
            query = query.Where(operation =>
                operation.CorrelationId.Contains(searchText) ||
                (operation.ResourceId != null && operation.ResourceId.Contains(searchText)) ||
                (operation.IdempotencyKey != null && operation.IdempotencyKey.Contains(searchText)) ||
                (operation.FailureMessage != null && operation.FailureMessage.Contains(searchText)));
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        return await query
            .OrderByDescending(operation => operation.StartedAt)
            .Take(take)
            .Select(operation => new ClinicalFhirOperationRecord
            {
                Id = operation.Id,
                CorrelationId = operation.CorrelationId,
                ETag = operation.ETag,
                FailureKind = operation.FailureKind,
                FailureMessage = operation.FailureMessage,
                IdempotencyKey = operation.IdempotencyKey,
                Interaction = operation.Interaction,
                OutcomeJson = operation.OutcomeJson,
                RequestJson = operation.RequestJson,
                ResourceId = operation.ResourceId,
                ResourceType = operation.ResourceType,
                ResponseJson = operation.ResponseJson,
                ResponseStatusCode = operation.ResponseStatusCode,
                ResultCount = operation.ResultCount,
                RetryCount = operation.RetryCount,
                StartedAt = operation.StartedAt,
                CompletedAt = operation.CompletedAt,
                Status = operation.Status,
                RemoteVersionId = operation.RemoteVersionId
            })
            .ToListAsync(cancellationToken);
    }

    public Task<ClinicalOperationsConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        ClinicalOperationsConfiguration configuration = new()
        {
            AllowedResourceTypes = _options.AllowedResourceTypes
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            AllowedOperations = _options.AllowedOperations
                .Select(value => Enum.TryParse<FhirInteractionType>(value, true, out FhirInteractionType interaction)
                    ? (FhirInteractionType?)interaction
                    : null)
                .OfType<FhirInteractionType>()
                .Distinct()
                .OrderBy(value => value)
                .ToArray()
        };

        return Task.FromResult(configuration);
    }

    private async Task<ClinicalFhirOperation?> FindExistingIdempotentOperationAsync(FhirOperationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return null;
        }

        return await dbContext.FhirOperations
            .FirstOrDefaultAsync(operation =>
                operation.IdempotencyKey == request.IdempotencyKey &&
                operation.ResourceType == request.ResourceType &&
                operation.ResourceId == request.ResourceId &&
                operation.Interaction == request.Interaction,
                cancellationToken);
    }

    private bool IsAllowedOperation(FhirInteractionType interaction) =>
        _options.AllowedOperations.Any(value => string.Equals(value, interaction.ToString(), StringComparison.OrdinalIgnoreCase));

    private bool IsAllowedResourceType(string resourceType) =>
        _options.AllowedResourceTypes.Any(value => string.Equals(value, resourceType, StringComparison.OrdinalIgnoreCase));

    private static string BuildDeniedMessage(string resourceType, FhirInteractionType interaction) =>
        $"Operation '{interaction}' is not allowed or resource type '{resourceType}' is not enabled.";

    private static FhirFailureKind ClassifyFailure(Exception exception) => exception switch
    {
        TaskCanceledException => FhirFailureKind.Timeout,
        HttpRequestException httpException when httpException.StatusCode == HttpStatusCode.Conflict => FhirFailureKind.Conflict,
        HttpRequestException httpException when httpException.StatusCode == HttpStatusCode.NotFound => FhirFailureKind.NotFound,
        HttpRequestException => FhirFailureKind.Transport,
        InvalidOperationException => FhirFailureKind.Validation,
        _ => FhirFailureKind.Unexpected
    };

    private static FhirOperationResponse Map(ClinicalFhirOperation operation) => new()
    {
        OperationId = operation.Id,
        CorrelationId = operation.CorrelationId,
        Status = operation.Status,
        FailureKind = operation.FailureKind,
        FailureMessage = operation.FailureMessage,
        ResponseStatusCode = operation.ResponseStatusCode,
        ResultCount = operation.ResultCount,
        ResponseJson = operation.ResponseJson,
        OutcomeJson = operation.OutcomeJson,
        RemoteVersionId = operation.RemoteVersionId,
        ETag = operation.ETag,
        StartedAt = operation.StartedAt,
        CompletedAt = operation.CompletedAt
    };
}
