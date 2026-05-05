using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Hl7.Fhir.Rest;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalConnectorHealthService(
    IFhirGateway gateway,
    ClinicalDbContext dbContext,
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<ClinicalOptions> options,
    ILogger<ClinicalConnectorHealthService> logger) : IClinicalConnectorHealthService
{
    private readonly ClinicalOptions _options = options.CurrentValue;

    public async Task<ConnectorHealthReport> ProbeAsync(ClinicalTenantSettings? tenantSettings = null, CancellationToken cancellationToken = default)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        string endpoint = tenantSettings?.EndpointOverride ?? _options.Fhir.BaseUrl;

        if (!_options.Enabled)
        {
            return new ConnectorHealthReport
            {
                CheckedAt = checkedAt,
                Endpoint = endpoint,
                FailureMessage = "Clinical connector is disabled in configuration.",
                IsConfigured = false,
                State = ConnectorHealthState.Unknown
            };
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new ConnectorHealthReport
            {
                CheckedAt = checkedAt,
                Endpoint = string.Empty,
                FailureMessage = "Clinical FHIR endpoint is not configured.",
                IsConfigured = false,
                State = ConnectorHealthState.Unreachable
            };
        }

        ConnectorHealthReport report;

        try
        {
            FhirCapabilitySummary summary = await gateway.GetCapabilitySummaryAsync(
                Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? overrideEndpoint) ? overrideEndpoint : null,
                cancellationToken);

            report = new ConnectorHealthReport
            {
                CheckedAt = checkedAt,
                Endpoint = summary.Endpoint,
                FhirVersion = summary.FhirVersion,
                IsConfigured = true,
                IsReachable = true,
                SoftwareName = summary.SoftwareName,
                SoftwareVersion = summary.SoftwareVersion,
                State = summary.SupportedResourceTypes.Count > 0 ? ConnectorHealthState.Healthy : ConnectorHealthState.Degraded,
                SupportedResourceTypes = summary.SupportedResourceTypes
            };
        }
        catch (Exception exception) when (exception is FhirOperationException or HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Clinical connector probe failed for endpoint {Endpoint}.", endpoint);

            report = new ConnectorHealthReport
            {
                CheckedAt = checkedAt,
                Endpoint = endpoint,
                FailureMessage = exception.Message,
                IsConfigured = true,
                IsReachable = false,
                State = ConnectorHealthState.Unreachable
            };
        }

        await TryPersistSnapshotAsync(report, cancellationToken);
        return report;
    }

    private async Task TryPersistSnapshotAsync(ConnectorHealthReport report, CancellationToken cancellationToken)
    {
        string? tenantId = requestContext.TenantId ?? httpContextAccessor.HttpContext?.GetTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        dbContext.ConnectorHealthSnapshots.Add(new ConnectorHealthSnapshot
        {
            CheckedAt = report.CheckedAt,
            Endpoint = report.Endpoint,
            FailureMessage = report.FailureMessage,
            FhirVersion = report.FhirVersion,
            IsReachable = report.IsReachable,
            SoftwareName = report.SoftwareName,
            SoftwareVersion = report.SoftwareVersion,
            State = report.State,
            SupportedResourceTypes = report.SupportedResourceTypes.Count == 0 ? null : string.Join(',', report.SupportedResourceTypes)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
