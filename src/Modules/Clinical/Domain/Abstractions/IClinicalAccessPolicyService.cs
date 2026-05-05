using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalAccessPolicyService
{
    Task<ClinicalAccessPolicy> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task<bool> CanExecuteAsync(FhirOperationRequest request, CancellationToken cancellationToken = default);
}
