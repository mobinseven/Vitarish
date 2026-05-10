using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IWorkflowPolicyEngine
{
    Task<IReadOnlyList<WorkflowValidationResult>> EvaluateAsync(
        string actorId,
        IReadOnlyList<string> actorRoles,
        ClinicalActionType actionType,
        ClinicalWorkflow? workflow,
        WorkflowPolicyDefinition policy,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
