using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IValidationCoordinator
{
    Task<IReadOnlyList<WorkflowValidationResult>> ValidateAsync(
        ClinicalActionType actionType,
        object? payload,
        WorkflowPolicyDefinition policy,
        string patientFhirReference,
        string? encounterFhirReference,
        CancellationToken cancellationToken = default);
}
