using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalConnectorHealthService
{
    Task<ConnectorHealthReport> ProbeAsync(ClinicalTenantSettings? tenantSettings = null, CancellationToken cancellationToken = default);
}
