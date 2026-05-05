using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalOperationsService
{
    Task<FhirOperationResponse> ExecuteAsync(FhirOperationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalFhirOperationRecord>> ListAsync(ClinicalFhirOperationFilter filter, CancellationToken cancellationToken = default);
    Task<ClinicalOperationsConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}
