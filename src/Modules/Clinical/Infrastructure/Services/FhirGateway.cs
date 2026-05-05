using System.Reflection;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;
using Bundle = Hl7.Fhir.Model.Bundle;
using CapabilityStatement = Hl7.Fhir.Model.CapabilityStatement;
using OperationOutcome = Hl7.Fhir.Model.OperationOutcome;
using Parameters = Hl7.Fhir.Model.Parameters;
using Resource = Hl7.Fhir.Model.Resource;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class FhirGateway(IOptionsMonitor<ClinicalOptions> options) : IFhirGateway
{
    private readonly ClinicalOptions _options = options.CurrentValue;
    private static readonly FhirJsonParser Parser = new();
    private static readonly FhirJsonSerializer Serializer = new();

    public async Task<FhirCapabilitySummary> GetCapabilitySummaryAsync(Uri? endpointOverride = null, CancellationToken cancellationToken = default)
    {
        Uri endpoint = ResolveEndpoint(endpointOverride);
        FhirClient client = CreateClient(endpoint);
        cancellationToken.ThrowIfCancellationRequested();
        CapabilityStatement capabilityStatement = await client.CapabilityStatementAsync();

        IReadOnlyList<string> supportedResourceTypes = capabilityStatement.Rest?
            .SelectMany(rest => rest.Resource ?? [])
            .Select(resource => resource.Type?.ToString())
            .OfType<string>()
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        return new FhirCapabilitySummary
        {
            Endpoint = endpoint.ToString().TrimEnd('/'),
            FhirVersion = capabilityStatement.FhirVersion?.ToString(),
            SoftwareName = capabilityStatement.Software?.Name,
            SoftwareVersion = capabilityStatement.Software?.Version,
            SupportedResourceTypes = supportedResourceTypes
        };
    }

    public async Task<FhirOperationResponse> ExecuteAsync(FhirOperationRequest request, Uri? endpointOverride = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceType);

        Uri endpoint = ResolveEndpoint(endpointOverride);
        FhirClient client = CreateClient(endpoint);
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;

        Resource? resource = null;
        Bundle? bundle = null;
        OperationOutcome? outcome = null;

        switch (request.Interaction)
        {
            case FhirInteractionType.Create:
                resource = await ExecuteCreateAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Read:
                resource = await ExecuteReadAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Update:
                resource = await ExecuteUpdateAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Patch:
                resource = await ExecutePatchAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Delete:
                await ExecuteDeleteAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Search:
                bundle = await ExecuteSearchAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.Validate:
                outcome = await ExecuteValidateAsync(client, request, cancellationToken);
                break;
            case FhirInteractionType.History:
                bundle = await ExecuteHistoryAsync(client, request, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"FHIR interaction '{request.Interaction}' is not supported.");
        }

        return new FhirOperationResponse
        {
            CorrelationId = request.CorrelationId ?? string.Empty,
            Status = FhirOperationStatus.Succeeded,
            FailureKind = FhirFailureKind.None,
            ResponseStatusCode = ParseStatusCode(client.LastResult?.Status),
            ResultCount = bundle?.Entry?.Count,
            ResponseJson = resource is not null ? Serializer.SerializeToString(resource) : bundle is not null ? Serializer.SerializeToString(bundle) : null,
            OutcomeJson = outcome is null ? null : Serializer.SerializeToString(outcome),
            RemoteVersionId = resource?.Meta?.VersionId ?? outcome?.Meta?.VersionId,
            ETag = client.LastResult?.Etag,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }

    private Task<Resource> ExecuteCreateAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        Resource resource = ParseResourcePayload(request, requireResourceId: false);
        cancellationToken.ThrowIfCancellationRequested();
        return client.CreateAsync(resource);
    }

    private Task<Resource> ExecuteReadAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        string identity = BuildIdentity(request, requireResourceId: true);
        return client.ReadAsync<Resource>(identity, null, null, cancellationToken);
    }

    private Task<Resource> ExecuteUpdateAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        Resource resource = ParseResourcePayload(request, requireResourceId: true);
        cancellationToken.ThrowIfCancellationRequested();
        return client.UpdateAsync(resource);
    }

    private async Task<Resource> ExecutePatchAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        Type resourceType = ResolveResourceClrType(request.ResourceType);
        MethodInfo method = typeof(FhirGateway)
            .GetMethod(nameof(PatchAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resourceType);

        string resourceId = request.ResourceId
            ?? throw new InvalidOperationException("Patch operations require a resource id.");
        string payloadJson = request.PayloadJson
            ?? throw new InvalidOperationException("Patch operations require a JSON payload.");

        return await (Task<Resource>)method.Invoke(null, [client, resourceId, payloadJson, cancellationToken])!;
    }

    private Task ExecuteDeleteAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        string identity = BuildIdentity(request, requireResourceId: true);
        cancellationToken.ThrowIfCancellationRequested();
        return client.DeleteAsync(identity);
    }

    private Task<Bundle> ExecuteSearchAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        Type resourceType = ResolveResourceClrType(request.ResourceType);
        SearchParams searchParams = BuildSearchParams(request.Query);
        MethodInfo method = typeof(FhirGateway)
            .GetMethod(nameof(SearchAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resourceType);

        return (Task<Bundle>)method.Invoke(null, [client, searchParams, cancellationToken])!;
    }

    private Task<OperationOutcome> ExecuteValidateAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
    {
        Parameters parameters = new();
        if (!string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            Resource resource = ParseResourcePayload(request, requireResourceId: false);
            parameters.Parameter.Add(new Parameters.ParameterComponent
            {
                Name = "resource",
                Resource = resource
            });
        }

        Task<Resource> validationTask = string.IsNullOrWhiteSpace(request.ResourceId)
            ? client.TypeOperationAsync(request.ResourceType, "$validate", parameters, false, cancellationToken)
            : client.InstanceOperationAsync(new Uri(BuildIdentity(request, requireResourceId: true), UriKind.Relative), "$validate", parameters, false, cancellationToken);

        return AwaitValidationOutcomeAsync(validationTask);
    }

    private Task<Bundle> ExecuteHistoryAsync(FhirClient client, FhirOperationRequest request, CancellationToken cancellationToken)
        => request.ResourceId is null
            ? client.TypeHistoryAsync(request.ResourceType, null, null, null, cancellationToken)
            : client.HistoryAsync(BuildIdentity(request, requireResourceId: true), null, null, null, cancellationToken);

    private FhirClient CreateClient(Uri endpoint)
    {
        FhirClientSettings settings = new()
        {
            Timeout = Math.Max(_options.Fhir.TimeoutSeconds, 1) * 1000,
            PreferredFormat = ResourceFormat.Json,
            VerifyFhirVersion = false
        };

        return new FhirClient(endpoint, settings);
    }

    private static SearchParams BuildSearchParams(string? query)
    {
        SearchParams searchParams = new();
        if (string.IsNullOrWhiteSpace(query))
        {
            return searchParams;
        }

        foreach (string condition in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            _ = searchParams.Where(condition);
        }

        return searchParams;
    }

    private static Task<Bundle> SearchAsyncGeneric<TResource>(FhirClient client, SearchParams searchParams, CancellationToken cancellationToken)
        where TResource : Resource =>
        client.SearchAsync<TResource>(searchParams, cancellationToken);

    private static async Task<Resource> PatchAsyncGeneric<TResource>(FhirClient client, string id, string patchDocument, CancellationToken cancellationToken)
        where TResource : Resource
    {
        TResource? resource = await client.PatchAsync<TResource>(id, patchDocument, ResourceFormat.Json, cancellationToken);
        return resource ?? throw new InvalidOperationException("Patch operation did not return a resource.");
    }

    private static async Task<OperationOutcome> AwaitValidationOutcomeAsync(Task<Resource> validationTask)
    {
        Resource resource = await validationTask;
        return resource as OperationOutcome ?? throw new InvalidOperationException("FHIR validation did not return an OperationOutcome.");
    }

    private Resource ParseResourcePayload(FhirOperationRequest request, bool requireResourceId)
    {
        Resource resource = Parser.Parse<Resource>(request.PayloadJson ?? throw new InvalidOperationException($"{request.Interaction} operations require a JSON payload."));

        if (!string.Equals(resource.TypeName, request.ResourceType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Payload resource type '{resource.TypeName}' does not match requested type '{request.ResourceType}'.");
        }

        if (requireResourceId)
        {
            string resourceId = request.ResourceId ?? resource.Id ?? throw new InvalidOperationException($"{request.Interaction} operations require a resource id.");
            resource.Id = resourceId;
        }

        return resource;
    }

    private static string BuildIdentity(FhirOperationRequest request, bool requireResourceId)
    {
        if (!requireResourceId)
        {
            return request.ResourceType;
        }

        if (string.IsNullOrWhiteSpace(request.ResourceId))
        {
            throw new InvalidOperationException($"{request.Interaction} operations require a resource id.");
        }

        return $"{request.ResourceType}/{request.ResourceId}";
    }

    private static Type ResolveResourceClrType(string resourceType)
    {
        Type? type = typeof(Resource).Assembly.GetType($"Hl7.Fhir.Model.{resourceType}", false, true);
        if (type is null || !typeof(Resource).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Unknown FHIR resource type '{resourceType}'.");
        }

        return type;
    }

    private static int? ParseStatusCode(string? status) =>
        int.TryParse(status, out int code) ? code : null;

    private Uri ResolveEndpoint(Uri? endpointOverride)
    {
        Uri? endpoint = endpointOverride;
        if (endpoint is null && Uri.TryCreate(_options.Fhir.BaseUrl, UriKind.Absolute, out Uri? configuredEndpoint))
        {
            endpoint = configuredEndpoint;
        }

        return endpoint ?? throw new InvalidOperationException("Clinical FHIR endpoint is not configured.");
    }
}
