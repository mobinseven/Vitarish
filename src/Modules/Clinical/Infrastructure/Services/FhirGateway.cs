using System.Net.Http;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class FhirGateway(IOptionsMonitor<ClinicalOptions> options) : IFhirGateway
{
    private readonly ClinicalOptions _options = options.CurrentValue;

    public async Task<FhirCapabilitySummary> GetCapabilitySummaryAsync(Uri? endpointOverride = null, CancellationToken cancellationToken = default)
    {
        Uri endpoint = ResolveEndpoint(endpointOverride);
        FhirClientSettings settings = new()
        {
            Timeout = Math.Max(_options.Fhir.TimeoutSeconds, 1) * 1000,
            PreferredFormat = ResourceFormat.Json,
            VerifyFhirVersion = false
        };

        FhirClient client = new(endpoint, settings);
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
