using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IFhirResourceMapper
{
    Task<IReadOnlyList<FhirMappedResource>> MapAsync(ClinicalActionType actionType, object payload, string patientFhirReference, string? encounterFhirReference, CancellationToken cancellationToken = default);
}
