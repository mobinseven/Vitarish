using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IFhirGateway
{
    Task<FhirCapabilitySummary> GetCapabilitySummaryAsync(Uri? endpointOverride = null, CancellationToken cancellationToken = default);
}
